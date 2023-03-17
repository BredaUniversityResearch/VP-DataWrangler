using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using CommonLogging;

namespace CameraControlOverEthernet
{
	public class CameraControlServer
	{
		public static readonly IPAddress DiscoveryMulticastAddress = IPAddress.Parse("224.0.0.69");
		public const int DiscoveryMulticastPort = 49069;
		public static readonly IPEndPoint DiscoveryMulticastEndpoint = new IPEndPoint(DiscoveryMulticastAddress, DiscoveryMulticastPort);
		public static readonly TimeSpan DiscoveryMulticastInterval = TimeSpan.FromSeconds(1);
		public const int ConnectionPort = 49070;

		private readonly TcpListener m_connectionListener = new TcpListener(IPAddress.Any, ConnectionPort);
		private readonly UdpClient m_discoveryBroadcaster = new UdpClient();

		private CancellationTokenSource m_cancellationTokenSource = new CancellationTokenSource();

		private Task? m_discoveryBroadcastTask = null;
		private Task? m_connectAcceptTask = null;
		private int m_serverIdentifier = Random.Shared.Next();

		private List<TcpClient> m_connectedClients = new List<TcpClient>();

		public void Start()
		{
			m_discoveryBroadcastTask = new Task(BackgroundDiscoveryBroadcastTask);
			m_discoveryBroadcastTask.Start();

			m_connectionListener.Start();
			m_connectAcceptTask = new Task(BackgroundAcceptConnections);
			Logger.LogVerbose("CCServer", $"Starting Camera Control Server. Using Multicast endpoint {DiscoveryMulticastEndpoint}. Listening on {m_connectionListener.LocalEndpoint}");
		}

		private async void BackgroundDiscoveryBroadcastTask()
		{
			byte[] buffer = new byte[128];
			while (!m_cancellationTokenSource.IsCancellationRequested)
			{
				await Task.Delay(DiscoveryMulticastInterval, m_cancellationTokenSource.Token);
				if (!m_cancellationTokenSource.IsCancellationRequested)
				{
					MemoryStream ms = new MemoryStream(buffer, 0, buffer.Length, true, false);
					using (BinaryWriter writer = new BinaryWriter(ms, Encoding.ASCII, true))
					{
						CameraControlTransport.Write(new CameraControlDiscoveryPacket(m_serverIdentifier, ConnectionPort), writer);
					}
					m_discoveryBroadcaster.Send(new ReadOnlySpan<byte>(buffer, 0, (int)ms.Position), DiscoveryMulticastEndpoint);
				}
			}
		}

		private async void BackgroundAcceptConnections()
		{
			while (!m_cancellationTokenSource.IsCancellationRequested)
			{
				TcpClient client = await m_connectionListener.AcceptTcpClientAsync(m_cancellationTokenSource.Token);
				if (!m_cancellationTokenSource.IsCancellationRequested)
				{
					lock (m_connectedClients)
					{
						m_connectedClients.Add(client);
					}
				}
			}
		}
	}
}