using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using CommonLogging;

namespace CameraControlOverEthernet
{
	public class NetworkedDeviceAPIClient
	{
		private class ServerConnection
		{
			public readonly int ServerIdentifier;
			public readonly TcpClient Socket;
            public Task? ReceiveDataTask;
            public CancellationTokenSource ReceiveDataCancellationSource = new CancellationTokenSource();
            public bool IsInConnectionProcess = true;
            public DateTime LastActivityTime = DateTime.Now;

			public ServerConnection(int a_serverIdentifier, TcpClient a_socket)
			{
				ServerIdentifier = a_serverIdentifier;
				Socket = a_socket;
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


		private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(5);
		private static readonly TimeSpan HeartbeatSendInterval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan InactivityDisconnectTime = TimeSpan.FromSeconds(15);

		private List<ServerConnection> m_activeClients = new List<ServerConnection>();
		private UdpClient m_discoveryReceiver = new UdpClient(NetworkedDeviceAPIServer.DiscoveryMulticastPort);

		private CancellationTokenSource m_stopListeningToken = new CancellationTokenSource();
		private Task? m_listenTask;
		private Task? m_connectTask;

		private ConcurrentQueue<KeyValuePair<int, IPEndPoint>> m_receivedServerBroadcastQueue = new();
		private AutoResetEvent m_receivedServerBroadcastEvent = new(false);
		private DateTime m_lastHeartbeatSendTime = DateTime.UtcNow;

		public event Action<int> OnConnected = delegate { };
		public event Action<int> OnDisconnected = delegate { };

        private BlockingCollection<QueuedPacketEntry> m_receivedPacketQueue = new BlockingCollection<QueuedPacketEntry>();

		public void StartListenForServer()
		{
			m_discoveryReceiver.Client.ReceiveTimeout = 1000;
			NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
			foreach (NetworkInterface adapter in nics)
			{
				IPInterfaceProperties ipProperties = adapter.GetIPProperties();
				if (adapter.GetIPProperties().MulticastAddresses.Count > 0 &&
					adapter.SupportsMulticast &&
					adapter.OperationalStatus == OperationalStatus.Up &&
					adapter.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
					adapter.Name.StartsWith("vEthernet") == false)
				{
					foreach (UnicastIPAddressInformation addr in ipProperties.UnicastAddresses)
					{
						if (addr.Address.AddressFamily == AddressFamily.InterNetwork)
						{
							m_discoveryReceiver.JoinMulticastGroup(NetworkedDeviceAPIServer.DiscoveryMulticastAddress, addr.Address);
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
				IPEndPoint remoteEndPoint = new IPEndPoint(NetworkedDeviceAPIServer.DiscoveryMulticastAddress, NetworkedDeviceAPIServer.DiscoveryMulticastPort);
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
								INetworkAPIPacket? packet = NetworkApiTransport.TryRead(reader);
								if (packet != null)
								{
									if (packet is NetworkAPIDiscoveryPacket discoveryPacket)
									{
										if (discoveryPacket.MagicBits == NetworkAPIDiscoveryPacket.ExpectedMagicBits)
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
                    ServerConnection connection = new ServerConnection(a_serverIdentifier, client);
                    connection.ReceiveDataTask = Task.Run(() => BackgroundReceiveData(connection), connection.ReceiveDataCancellationSource.Token);

					lock (m_activeClients)
					{
						m_activeClients.Add(connection);
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

        private void BackgroundReceiveData(ServerConnection a_targetConnection)
        {
            try
			{
				byte[] receiveBuffer = new byte[8192];
				int bytesFromLastReceive = 0;
                a_targetConnection.Socket.GetStream().ReadTimeout = 1000;

				while (a_targetConnection.Socket.Connected)
				{
					if (DateTime.UtcNow - a_targetConnection.LastActivityTime > InactivityDisconnectTime)
					{
						Logger.LogVerbose("NetworkDeviceAPI", $"Dropping server at {a_targetConnection.Socket.Client.RemoteEndPoint} due to inactivity");
						a_targetConnection.Socket.Client.Close();
						break;
					}

					int bytesReceived = 0;
					try
					{
						bytesReceived = a_targetConnection.Socket.GetStream().Read(receiveBuffer, bytesFromLastReceive, (int) receiveBuffer.Length - bytesFromLastReceive);
					}
					catch (IOException ex)
					{
						if (ex.InnerException is SocketException soEx)
						{
							if (soEx.SocketErrorCode == SocketError.ConnectionAborted ||
							    soEx.SocketErrorCode == SocketError.ConnectionReset)
							{
								Logger.LogVerbose("NetworkDeviceAPI", $"Dropping client at {a_targetConnection.Socket.Client.RemoteEndPoint} due to connection abort");
                                a_targetConnection.Socket.Close();
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
									Logger.LogVerbose("NetworkDeviceAPI", $"Received Packet From Client {a_targetConnection.Socket.Client.RemoteEndPoint} of type {packet.GetType()}");
									m_receivedPacketQueue.Add(new QueuedPacketEntry(packet, a_targetConnection.ServerIdentifier));
									a_targetConnection.LastActivityTime = DateTime.UtcNow;
									
								}
								else
								{
									bytesFromLastReceive = (int) (ms.Length - packetStart);
									Array.Copy(receiveBuffer, packetStart, receiveBuffer, 0, bytesFromLastReceive);
									Logger.LogVerbose("NetworkDeviceAPI", $"Failed to deserialize data from client {a_targetConnection.Socket.Client.RemoteEndPoint} moving {bytesFromLastReceive} bytes over to next receive cycle");
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

		public void SendPacket(INetworkAPIPacket a_packet)
		{
			byte[] bufferBytes = new byte[128];
			using MemoryStream ms = new MemoryStream(bufferBytes, true);
			using BinaryWriter writer = new BinaryWriter(ms, Encoding.ASCII, true);
			NetworkApiTransport.Write(a_packet, writer);

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
				SendPacket(new NetworkAPIHeartbeat());
				m_lastHeartbeatSendTime = DateTime.UtcNow;
			}
		}
	}
}
