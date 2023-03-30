using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using CommonLogging;

namespace CameraControlOverEthernet
{
	public class CameraControlNetworkTransmitter
	{
		private class ServerConnection
		{
			public readonly int ServerIdentifier;
			public readonly TcpClient Socket;

			public ServerConnection(int a_serverIdentifier, TcpClient a_socket)
			{
				ServerIdentifier = a_serverIdentifier;
				Socket = a_socket;
			}
		};

		private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);
		private static readonly TimeSpan HeartbeatSendInterval = TimeSpan.FromSeconds(5);

		private List<ServerConnection> m_activeClients = new List<ServerConnection>();
		private UdpClient m_discoveryReceiver = new UdpClient(CameraControlNetworkReceiver.DiscoveryMulticastPort);

		private CancellationTokenSource m_stopListeningToken = new CancellationTokenSource();
		private Task? m_listenTask;
		private Task? m_connectTask;

		private ConcurrentQueue<KeyValuePair<int, IPEndPoint>> m_receivedServerBroadcastQueue = new();
		private AutoResetEvent m_receivedServerBroadcastEvent = new(false);
		private DateTime m_lastHeartbeatSendTime = DateTime.UtcNow;

		public event Action<int> OnConnected = delegate { };
		public event Action<int> OnDisconnected = delegate { };

		public void StartListenForServer()
		{
			m_discoveryReceiver.Client.ReceiveTimeout = 1000;
			NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
			foreach (NetworkInterface adapter in nics)
			{
				IPInterfaceProperties ip_properties = adapter.GetIPProperties();
				if (adapter.GetIPProperties().MulticastAddresses.Count > 0 &&
				    adapter.SupportsMulticast &&
				    adapter.OperationalStatus == OperationalStatus.Up)
				{
					foreach (UnicastIPAddressInformation addr in ip_properties.UnicastAddresses)
					{
						if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
						{
							m_discoveryReceiver.JoinMulticastGroup(CameraControlNetworkReceiver.DiscoveryMulticastAddress, addr.Address);
						}
					}
				}
			} 


			m_listenTask = new Task(BackgroundListenForServer);
			m_listenTask.Start();

			m_connectTask = new Task(BackgroundConnectToServer);
			m_connectTask.Start();
		}

		private void BackgroundListenForServer()
		{
			while (!m_stopListeningToken.IsCancellationRequested)
			{
				IPEndPoint remoteEndPoint = new IPEndPoint(CameraControlNetworkReceiver.DiscoveryMulticastAddress, CameraControlNetworkReceiver.DiscoveryMulticastPort);
				byte[]? buffer = null;
				try
				{
					buffer = m_discoveryReceiver.Receive(ref remoteEndPoint);
				}
				catch (SocketException ex)
				{
					if (ex.SocketErrorCode != SocketError.TimedOut)
					{
						throw;
					}
				}

				if (!m_stopListeningToken.IsCancellationRequested && buffer?.Length > 0)
				{
					Logger.LogVerbose("CCClient", $"Received multicast packet from {remoteEndPoint}");
					using (MemoryStream ms = new MemoryStream(buffer))
					{
						using (BinaryReader reader = new BinaryReader(ms, Encoding.ASCII, true))
						{
							while (ms.Position < ms.Length)
							{
								ICameraControlPacket? packet = CameraControlTransport.TryRead(reader);
								if (packet != null)
								{
									if (packet is CameraControlDiscoveryPacket discoveryPacket)
									{
										if (discoveryPacket.MagicBits == CameraControlDiscoveryPacket.ExpectedMagicBits)
										{
											m_receivedServerBroadcastQueue.Enqueue(new KeyValuePair<int, IPEndPoint>(discoveryPacket.ServerIdentifier, 
												new IPEndPoint(remoteEndPoint.Address, discoveryPacket.TargetPort)));
										}
									}
								}
								else
								{
									Logger.LogVerbose("CCClient", "Received malformed packet");
								}
							}

							if (ms.Position != ms.Length)
							{
								throw new Exception("Deserialized partial data?");
							}
						}
					}
				}
			}
		}
		
		private async void BackgroundConnectToServer()
		{
			while (!m_stopListeningToken.IsCancellationRequested)
			{
				m_receivedServerBroadcastEvent.WaitOne(TimeSpan.FromSeconds(1));
				while (m_receivedServerBroadcastQueue.TryDequeue(out KeyValuePair<int, IPEndPoint> endPoint))
				{
					await TryConnect(endPoint.Key, endPoint.Value);
				}
			}
		}

		private async Task TryConnect(int a_serverIdentifier, IPEndPoint a_endPointValue)
		{
			if (IsConnectedToServer(a_serverIdentifier))
			{
				return;
			}

			TcpClient client = new TcpClient();
			CancellationTokenSource connectTimeout = new CancellationTokenSource(ConnectTimeout);
			try
			{
				await client.ConnectAsync(a_endPointValue, connectTimeout.Token);
				if (client.Connected)
				{
					lock (m_activeClients)
					{
						m_activeClients.Add(new ServerConnection(a_serverIdentifier, client));
						Logger.LogVerbose("CCClient", $"Successfully connected to server {a_endPointValue}");
					}

					OnConnected(a_serverIdentifier);
				}
				else
				{
					throw new Exception("Failed to connect for some mysterious reason.");
				}
			}
			catch (SocketException ex)
			{
				if (ex.SocketErrorCode == SocketError.ConnectionRefused)
				{
					Logger.LogVerbose("CCClient", $"Failed to connect to server {a_endPointValue}. Server refused connection.");
				}
				else if (ex.SocketErrorCode == SocketError.NetworkUnreachable)
				{
					Logger.LogVerbose("CCClient", $"Failed to connect to server {a_endPointValue}. Network unreachable.");
				}
				else
				{
					throw;
				}
			}
			catch (OperationCanceledException)
			{
				Logger.LogVerbose("CCClient", $"Failed to connect to server {a_endPointValue}. Connection timed out.");
			}
		}

		private bool IsConnectedToServer(int a_serverIdentifier)
		{
			lock (m_activeClients)
			{
				foreach (ServerConnection conn in m_activeClients)
				{
					if (conn.ServerIdentifier == a_serverIdentifier)
					{
						return true;
					}
				}
			}

			return false;
		}

		public void SendPacket(ICameraControlPacket a_packet)
		{
			byte[] bufferBytes = new byte[128];
			using MemoryStream ms = new MemoryStream(bufferBytes,true);
			using BinaryWriter writer = new BinaryWriter(ms, Encoding.ASCII, true);
			CameraControlTransport.Write(a_packet, writer);

			CleanDisconnectedClients();

			lock (m_activeClients)
			{
				foreach (ServerConnection connectedServer in m_activeClients)
				{
					try
					{
						if (connectedServer.Socket.Connected)
						{
							connectedServer.Socket.GetStream().Write(bufferBytes, 0, (int)ms.Position);
						}
					}
					catch (IOException ex)
					{
						if (ex.InnerException != null && ex.InnerException is SocketException soEx)
						{
							if (soEx.SocketErrorCode == SocketError.ConnectionReset)
							{
								//Disconnect handle;
								connectedServer.Socket.Close();
							}
						}
						else
						{
							throw;
						}
					}
				}
			}
		}

		private void CleanDisconnectedClients()
		{
			lock (m_activeClients)
			{
				for (int i = m_activeClients.Count - 1; i >= 0; --i)
				{
					if (!m_activeClients[i].Socket.Connected)
					{
						OnDisconnected(m_activeClients[i].ServerIdentifier);
						m_activeClients.RemoveAt(i);
					}
				}
			}
		}

		public void Update()
		{
			CleanDisconnectedClients();
			if (DateTime.UtcNow - m_lastHeartbeatSendTime > HeartbeatSendInterval)
			{
				SendPacket(new CameraControlHeartbeat());
				m_lastHeartbeatSendTime = DateTime.UtcNow;
			}
		}
	}
}
