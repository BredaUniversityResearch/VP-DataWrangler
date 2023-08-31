using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using BlackmagicCameraControlData.CommandPackets;
using CommonLogging;

namespace CameraControlOverEthernet
{
	public class CameraControlNetworkReceiver
	{
		private class ClientConnection
		{
			public readonly TcpClient Client;
			public readonly int ConnectionId;
			public Task? ReceiveTask;
			public DateTime LastActivityTime;

			public ClientConnection(TcpClient a_client, int a_connectionId)
			{
				Client = a_client;
				ConnectionId = a_connectionId;
			}
		};

		private class QueuedPacketEntry
		{
			public readonly ICameraControlPacket Packet;
			public readonly int ConnectionId;

			public QueuedPacketEntry(ICameraControlPacket a_packet, int a_connectionId)
			{
				Packet = a_packet;
				ConnectionId = a_connectionId;
			}
		};

		public static readonly IPAddress DiscoveryMulticastAddress = IPAddress.Parse("224.0.1.69");
		public const int DiscoveryMulticastPort = 49069;
		public static readonly IPEndPoint DiscoveryMulticastEndpoint = new IPEndPoint(DiscoveryMulticastAddress, DiscoveryMulticastPort);
		public static readonly TimeSpan DiscoveryMulticastInterval = TimeSpan.FromSeconds(5);
		public static readonly TimeSpan InactivityDisconnectTime = TimeSpan.FromSeconds(15);

		private readonly TcpListener m_connectionListener = new TcpListener(IPAddress.Any, 0);
		private readonly UdpClient m_discoveryBroadcaster = new UdpClient();

		private CancellationTokenSource m_cancellationTokenSource = new CancellationTokenSource();

		private int m_lastConnectionId = 0;

		private Task? m_discoveryBroadcastTask = null;
		private Task? m_connectAcceptTask = null;
		private readonly int m_serverIdentifier = Random.Shared.Next();

		private readonly List<ClientConnection> m_connectedClients = new List<ClientConnection>();
		private BlockingCollection<QueuedPacketEntry> m_receivedPacketQueue = new BlockingCollection<QueuedPacketEntry>();

		public delegate void ClientDisconnectedDelegate(int a_connectionId);

		public event ClientDisconnectedDelegate OnClientDisconnected = delegate { };

		public void Start()
		{
			m_connectionListener.Start();

			m_discoveryBroadcastTask = new Task(BackgroundDiscoveryBroadcastTask);
			m_discoveryBroadcastTask.Start();

			m_connectAcceptTask = new Task(BackgroundAcceptConnections);
			m_connectAcceptTask.Start();
			Logger.LogVerbose("CCServer", $"Starting Camera Control Server. Using Multicast endpoint {DiscoveryMulticastEndpoint}. Listening on {m_connectionListener.LocalEndpoint}");
		}

		private async void BackgroundDiscoveryBroadcastTask()
		{
			NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
			foreach (NetworkInterface adapter in nics)
			{
				IPInterfaceProperties ipProps = adapter.GetIPProperties();
				if (adapter.GetIPProperties().MulticastAddresses.Count > 0 &&
				    adapter.SupportsMulticast &&
				    adapter.OperationalStatus == OperationalStatus.Up &&
				    adapter.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
				    adapter.Name.StartsWith("vEthernet") == false)
				{
					foreach (UnicastIPAddressInformation addr in ipProps.UnicastAddresses)
					{
						if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
						{
							m_discoveryBroadcaster.JoinMulticastGroup(DiscoveryMulticastAddress, addr.Address);
						}
					}
				}
			}

			byte[] buffer = new byte[128];
			while (!m_cancellationTokenSource.IsCancellationRequested)
			{
				MemoryStream ms = new MemoryStream(buffer, 0, buffer.Length, true, false);
				using (BinaryWriter writer = new BinaryWriter(ms, Encoding.ASCII, true))
				{
					CameraControlTransport.Write(new CameraControlDiscoveryPacket(m_serverIdentifier, ((IPEndPoint) m_connectionListener.LocalEndpoint).Port), writer);
				}

				m_discoveryBroadcaster.Send(new ReadOnlySpan<byte>(buffer, 0, (int) ms.Position), DiscoveryMulticastEndpoint);

				await Task.Delay(DiscoveryMulticastInterval, m_cancellationTokenSource.Token);
			}
		}

		private async void BackgroundAcceptConnections()
		{
			while (!m_cancellationTokenSource.IsCancellationRequested)
			{
				TcpClient client = await m_connectionListener.AcceptTcpClientAsync(m_cancellationTokenSource.Token);
				if (!m_cancellationTokenSource.IsCancellationRequested)
				{
					ClientConnection conn = new ClientConnection(client, ++m_lastConnectionId);
					conn.ReceiveTask = new Task(() => BackgroundReceiveData(conn));
					conn.ReceiveTask.Start();
					conn.LastActivityTime = DateTime.UtcNow;

					Logger.LogVerbose("CCServer", $"Client connected from {client.Client.RemoteEndPoint}");

					lock (m_connectedClients)
					{
						m_connectedClients.Add(conn);
					}
				}
			}
		}

		private void BackgroundReceiveData(ClientConnection a_client)
		{
			byte[] receiveBuffer = new byte[8192];
			int bytesFromLastReceive = 0;
			a_client.Client.GetStream().ReadTimeout = 1000;

			while (a_client.Client.Connected)
			{
				if (DateTime.UtcNow - a_client.LastActivityTime > InactivityDisconnectTime)
				{
					Logger.LogVerbose("CCServer", $"Dropping client at {a_client.Client.Client.RemoteEndPoint} due to inactivity");
					a_client.Client.Close();
					break;
				}

				int bytesReceived = 0;
				try
				{
					bytesReceived = a_client.Client.GetStream().Read(receiveBuffer, bytesFromLastReceive, (int) receiveBuffer.Length - bytesFromLastReceive);
				}
				catch (IOException ex)
				{
					if (ex.InnerException is SocketException soEx)
					{
						if (soEx.SocketErrorCode == SocketError.ConnectionAborted ||
						    soEx.SocketErrorCode == SocketError.ConnectionReset)
						{
							Logger.LogVerbose("CCServer", $"Dropping client at {a_client.Client.Client.RemoteEndPoint} due to connection abort");
							a_client.Client.Close();
						}
						else if (soEx.SocketErrorCode == SocketError.TimedOut)
						{
							//Swallow this since we setup a timeout.
						}
						else
						{
							throw;
						}
					}
					else
						throw;
				}

				if (bytesReceived > 0)
				{
					MemoryStream ms = new MemoryStream(receiveBuffer, 0, bytesReceived + bytesFromLastReceive, false);
					bytesFromLastReceive = 0;

					using (BinaryReader reader = new BinaryReader(ms, Encoding.ASCII))
					{
						while (ms.Position < ms.Length)
						{
							long packetStart = ms.Position;
							ICameraControlPacket? packet = CameraControlTransport.TryRead(reader);
							if (packet != null)
							{
								//Logger.LogVerbose("CCServer", $"Received Packet From Client {a_client.Client.Client.RemoteEndPoint} of type {packet.GetType()}");
								m_receivedPacketQueue.Add(new QueuedPacketEntry(packet, a_client.ConnectionId));
								a_client.LastActivityTime = DateTime.UtcNow;
							}
							else
							{
								bytesFromLastReceive = (int)(ms.Length - packetStart);
								Array.Copy(receiveBuffer, packetStart, receiveBuffer, 0, bytesFromLastReceive);
								Logger.LogVerbose("CCServer", $"Failed to deserialize data from client {a_client.Client.Client.RemoteEndPoint} moving {bytesFromLastReceive} bytes over to next receive cycle");
								break;
							}
						}
					}
				}
			}

			lock (m_connectedClients)
			{
				OnClientDisconnected(a_client.ConnectionId);
				m_connectedClients.Remove(a_client);
			}

			Logger.LogVerbose("CCServer", $"Dropped client. See previous message for details.");
		}

		public bool TryDequeueMessage([NotNullWhen(true)] out ICameraControlPacket? a_cameraControlPacket, out int a_connectionId, TimeSpan a_timeout, CancellationToken a_cancellationToken)
		{
			if (m_receivedPacketQueue.TryTake(out QueuedPacketEntry? entry, (int) a_timeout.TotalMilliseconds, a_cancellationToken))
			{
				a_cameraControlPacket = entry.Packet;
				a_connectionId = entry.ConnectionId;
				return true;
			}

			a_cameraControlPacket = null;
			a_connectionId = -1;
			return false;
		}

		public void SendMessageToConnection(int a_connectionId, ICameraControlPacket a_packetToSend)
		{
			lock (m_connectedClients)
			{
				ClientConnection? conn = m_connectedClients.Find((obj) => obj.ConnectionId == a_connectionId);
				if (conn != null)
				{
					byte[] bufferBytes = new byte[128];
					using MemoryStream ms = new MemoryStream(bufferBytes, true);
					using BinaryWriter writer = new BinaryWriter(ms, Encoding.ASCII, true);
					CameraControlTransport.Write(a_packetToSend, writer);

					conn.Client.GetStream().Write(bufferBytes, 0, (int)ms.Position);
				}
			}
		}
	}
}