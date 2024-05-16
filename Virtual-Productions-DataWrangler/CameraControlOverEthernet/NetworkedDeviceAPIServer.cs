using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using CommonLogging;

namespace CameraControlOverEthernet
{
	public class NetworkedDeviceAPIServer: IDisposable
	{
		private class ClientConnection
		{
			public readonly TcpClient Client;
			public readonly int ConnectionId;
            public readonly CancellationTokenSource ReceiveTaskCancellation = new CancellationTokenSource();
			public Task? ReceiveTask;
			public DateTime LastActivityTime;
			public bool IsInConnectionProcess = true;
			public NetworkAPIDeviceCapabilities? Capabilities = null;

			public ClientConnection(TcpClient a_client, int a_connectionId)
			{
				Client = a_client;
				ConnectionId = a_connectionId;
			}
		};

		private class QueuedPacketEntry
		{
			public readonly INetworkAPIPacket Packet;
			public readonly int ConnectionId;

			public QueuedPacketEntry(INetworkAPIPacket a_packet, int a_connectionId)
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
		private readonly List<INetworkAPIEventHandler> m_callbackEventHandlers = new List<INetworkAPIEventHandler>();

		private Task? m_backgroundDispatchTask = null;

		public void Dispose()
		{
            foreach (ClientConnection conn in m_connectedClients)
            {
                conn.ReceiveTaskCancellation.Cancel();
            }

            m_cancellationTokenSource.Cancel();

			if (!(m_discoveryBroadcastTask?.Wait(TimeSpan.FromMilliseconds(100)) ?? true) ||
			    !(m_connectAcceptTask?.Wait(TimeSpan.FromMilliseconds(100)) ?? true) ||
			    !(m_backgroundDispatchTask?.Wait(TimeSpan.FromMilliseconds(100)) ?? true))
			{
				throw new Exception("One or more subtasks failed to terminate within a reasonable amount of time");
			}

			m_discoveryBroadcastTask?.Dispose();
			m_connectAcceptTask?.Dispose();
			m_backgroundDispatchTask?.Dispose();

			m_discoveryBroadcaster.Dispose();
			m_cancellationTokenSource.Dispose();
			m_receivedPacketQueue.Dispose();
		}

		public void Start()
		{
			m_connectionListener.Start();

			m_discoveryBroadcastTask = new Task(BackgroundDiscoveryBroadcastTask);
			m_discoveryBroadcastTask.Start();

			m_connectAcceptTask = new Task(BackgroundAcceptConnections);
			m_connectAcceptTask.Start();

			m_backgroundDispatchTask = Task.Run(BackgroundDispatchReceivedEvents);

			Logger.LogVerbose("NetworkDeviceAPI", $"Starting Server. Using Multicast endpoint {DiscoveryMulticastEndpoint}. Listening on {m_connectionListener.LocalEndpoint}");
		}

		public void RegisterEventHandler(INetworkAPIEventHandler a_handler)
		{
			lock (m_callbackEventHandlers)
			{
				if (m_callbackEventHandlers.Contains(a_handler))
				{
					throw new Exception("Double registration of a packet handler");
				}

				m_callbackEventHandlers.Add(a_handler);
			}
		}

		public void UnregisterEventHandler(INetworkAPIEventHandler a_handler)
		{
			lock (m_callbackEventHandlers)
			{
				m_callbackEventHandlers.Remove(a_handler);
			}
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
					NetworkApiTransport.Write(new NetworkAPIDiscoveryPacket(m_serverIdentifier, ((IPEndPoint) m_connectionListener.LocalEndpoint).Port), writer);
				}

				m_discoveryBroadcaster.Send(new ReadOnlySpan<byte>(buffer, 0, (int) ms.Position), DiscoveryMulticastEndpoint);

				try
				{
					await Task.Delay(DiscoveryMulticastInterval, m_cancellationTokenSource.Token);
				}
				catch (TaskCanceledException)
				{
					return;
				}
			}
		}

		private async void BackgroundAcceptConnections()
		{
			while (!m_cancellationTokenSource.IsCancellationRequested)
			{
				TcpClient? client = null;
				try
				{
					client = await m_connectionListener.AcceptTcpClientAsync(m_cancellationTokenSource.Token);
				}
				catch (OperationCanceledException)
				{
				}

				if (!m_cancellationTokenSource.IsCancellationRequested && client != null)
				{
					ClientConnection conn = new ClientConnection(client, ++m_lastConnectionId);
					conn.ReceiveTask = new Task(() => BackgroundReceiveData(conn), conn.ReceiveTaskCancellation.Token);
					conn.ReceiveTask.Start();
					conn.LastActivityTime = DateTime.UtcNow;

					Logger.LogVerbose("NetworkDeviceAPI", $"Client connected from {client.Client.RemoteEndPoint}");

					lock (m_connectedClients)
					{
						m_connectedClients.Add(conn);
					}
				}
			}
		}

		private void BackgroundReceiveData(ClientConnection a_client)
		{
			try
			{
				byte[] receiveBuffer = new byte[8192];
				int bytesFromLastReceive = 0;
				a_client.Client.GetStream().ReadTimeout = 1000;

				while (a_client.Client.Connected)
				{
					if (DateTime.UtcNow - a_client.LastActivityTime > InactivityDisconnectTime)
					{
						Logger.LogVerbose("NetworkDeviceAPI", $"Dropping client at {a_client.Client.Client.RemoteEndPoint} due to inactivity. {((a_client.IsInConnectionProcess)? "Client was still in handshaking process." : "" )}");
						a_client.Client.Close();
						break;
					}

					int bytesReceived = 0;
					try
					{
						bytesReceived = a_client.Client.GetStream().ReadAsync(receiveBuffer, bytesFromLastReceive, (int) receiveBuffer.Length - bytesFromLastReceive, a_client.ReceiveTaskCancellation.Token).Result;
					}
					catch (IOException ex)
					{
						if (ex.InnerException is SocketException soEx)
						{
							if (soEx.SocketErrorCode == SocketError.ConnectionAborted ||
							    soEx.SocketErrorCode == SocketError.ConnectionReset)
							{
								Logger.LogVerbose("NetworkDeviceAPI", $"Dropping client at {a_client.Client.Client.RemoteEndPoint} due to connection abort");
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
								INetworkAPIPacket? packet = NetworkApiTransport.TryRead(reader);
								if (packet != null)
								{
									if (a_client.IsInConnectionProcess)
									{
										TryHandlePacketHandshakePhase(a_client, packet);
									}
									else
									{
										//Logger.LogVerbose("NetworkDeviceAPI", $"Received Packet From Client {a_client.Client.Client.RemoteEndPoint} of type {packet.GetType()}");
										m_receivedPacketQueue.Add(new QueuedPacketEntry(packet, a_client.ConnectionId));
										a_client.LastActivityTime = DateTime.UtcNow;
									}
								}
								else
								{
									bytesFromLastReceive = (int) (ms.Length - packetStart);
									Array.Copy(receiveBuffer, packetStart, receiveBuffer, 0, bytesFromLastReceive);
									Logger.LogVerbose("NetworkDeviceAPI", $"Failed to deserialize data from client {a_client.Client.Client.RemoteEndPoint} moving {bytesFromLastReceive} bytes over to next receive cycle");
									break;
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.LogError("NetworkDeviceAPI", $"Background Receive Thread terminated due to unhandled exception {ex.Message}");
			}
			finally
			{
				if (!a_client.IsInConnectionProcess)
				{
					lock (m_callbackEventHandlers)
					{
						foreach (INetworkAPIEventHandler handler in m_callbackEventHandlers)
						{
							handler.OnClientDisconnected(a_client.ConnectionId);
						}
					}
				}

				lock (m_connectedClients)
				{
					m_connectedClients.Remove(a_client);
				}
				Logger.LogVerbose("NetworkDeviceAPI", $"Dropped client. See previous message for details.");
			}
		}

		private void TryHandlePacketHandshakePhase(ClientConnection a_client, INetworkAPIPacket a_packet)
		{
			if (a_packet is NetworkAPIReportDeviceCapabilitiesPacket deviceCapabilitiesPacket)
			{
				a_client.Capabilities = new NetworkAPIDeviceCapabilities()
				{
					DeviceRole = (NetworkAPIDeviceCapabilities.EDeviceRole) deviceCapabilitiesPacket.DeviceRole
				};

				a_client.IsInConnectionProcess = false;
				a_client.LastActivityTime = DateTime.UtcNow;

				lock (m_callbackEventHandlers)
				{
					foreach (INetworkAPIEventHandler handler in m_callbackEventHandlers)
					{
						handler.OnClientConnected(a_client.ConnectionId, a_client.Capabilities);
					}
				}

				SendMessageToConnection(a_client.ConnectionId, new NetworkAPIHandshakeCompletePacket());

				Logger.LogVerbose("NetworkDeviceAPI", $"Handshake Completed for client {a_client.Client.Client.RemoteEndPoint}");

			}
			else if (a_packet is NetworkAPIHeartbeat)
			{
				//Ignore heartbeat packets for now.
			}
			else
			{
				Logger.LogWarning("NetworkDeviceAPI", $"Received packet {a_packet.GetType()} from client {a_client.ConnectionId} during handshake process");
			}
		}

		public bool TryDequeueMessage([NotNullWhen(true)] out INetworkAPIPacket? a_cameraControlPacket, out int a_connectionId, TimeSpan a_timeout, CancellationToken a_cancellationToken)
		{
			try
			{
				if (m_receivedPacketQueue.TryTake(out QueuedPacketEntry? entry, (int) a_timeout.TotalMilliseconds, a_cancellationToken))
				{
					a_cameraControlPacket = entry.Packet;
					a_connectionId = entry.ConnectionId;
					return true;
				}
			}
			catch (TaskCanceledException)
			{
			}
			catch (OperationCanceledException)
			{
			}

			a_cameraControlPacket = null;
			a_connectionId = -1;
			return false;
		}

		public void SendMessageToAllConnectedClients(INetworkAPIPacket a_packetToSend)
		{
			byte[] bufferBytes = new byte[128];
			using MemoryStream ms = new MemoryStream(bufferBytes, true);
			using BinaryWriter writer = new BinaryWriter(ms, Encoding.ASCII, true);
			NetworkApiTransport.Write(a_packetToSend, writer);

			lock (m_connectedClients)
			{
				foreach (ClientConnection conn in m_connectedClients)
				{
					conn.Client.GetStream().Write(bufferBytes, 0, (int) ms.Position);
				}
			}
		}

		public void SendMessageToConnection(int a_connectionId, INetworkAPIPacket a_packetToSend)
		{
			lock (m_connectedClients)
			{
				ClientConnection? conn = m_connectedClients.Find((obj) => obj.ConnectionId == a_connectionId);
				if (conn != null)
				{
					byte[] bufferBytes = new byte[128];
					using MemoryStream ms = new MemoryStream(bufferBytes, true);
					using BinaryWriter writer = new BinaryWriter(ms, Encoding.ASCII, true);
					NetworkApiTransport.Write(a_packetToSend, writer);

					conn.Client.GetStream().Write(bufferBytes, 0, (int)ms.Position);
				}
			}
		}

		private void BackgroundDispatchReceivedEvents()
		{
			while (!m_cancellationTokenSource.IsCancellationRequested)
			{
				if (TryDequeueMessage(out INetworkAPIPacket? packet, out int connectionId, TimeSpan.FromSeconds(10), m_cancellationTokenSource.Token))
				{
					lock (m_callbackEventHandlers)
					{
						foreach (INetworkAPIEventHandler handler in m_callbackEventHandlers)
						{
							handler.OnPacketReceived(packet, connectionId);
						}
					}
				}
			}
		}
	}
}