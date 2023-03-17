using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using CommonLogging;

namespace CameraControlOverEthernet
{
	public class CameraControlClient
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

		private List<ServerConnection> m_activeClients = new List<ServerConnection>();
		private UdpClient m_discoveryReceiver = new UdpClient(CameraControlServer.DiscoveryMulticastPort);

		private CancellationTokenSource m_stopListeningToken = new CancellationTokenSource();
		private Task? m_listenTask;
		private Task? m_connectTask;

		private ConcurrentQueue<KeyValuePair<int, IPEndPoint>> m_receivedServerBroadcastQueue = new ConcurrentQueue<KeyValuePair<int, IPEndPoint>>();
		private AutoResetEvent m_receivedServerBroadcastEvent = new AutoResetEvent(false);

		public void StartListenForServer()
		{
			m_discoveryReceiver.JoinMulticastGroup(CameraControlServer.DiscoveryMulticastAddress);
			m_discoveryReceiver.Client.ReceiveTimeout = 1000;

			m_listenTask = new Task(BackgroundListenForServer);
			m_listenTask.Start();

			m_connectTask = new Task(BackgroundConnectToServer);
			m_connectTask.Start();
		}

		private async void BackgroundListenForServer()
		{
			while (!m_stopListeningToken.IsCancellationRequested)
			{
				UdpReceiveResult result = await m_discoveryReceiver.ReceiveAsync(m_stopListeningToken.Token);
				if (!m_stopListeningToken.IsCancellationRequested)
				{
					Logger.LogVerbose("CCClient", $"Received multicast packet from {result.RemoteEndPoint}");
					using (MemoryStream ms = new MemoryStream(result.Buffer))
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
												new IPEndPoint(result.RemoteEndPoint.Address, discoveryPacket.TargetPort)));
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
				else
				{
					throw;
				}
			}
			catch (OperationCanceledException)
			{
				Logger.LogVerbose("CCClient", $"Failed to connect to server {a_endPointValue}");
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
	}
}
