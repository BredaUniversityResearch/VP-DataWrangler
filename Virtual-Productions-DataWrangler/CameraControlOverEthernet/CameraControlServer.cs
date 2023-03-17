using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;

namespace CameraControlOverEthernet
{
	public class CameraControlServer
	{
		private static readonly IPAddress DiscoveryMulticastAddress = IPAddress.Parse("224.0.0.69");
		private static readonly IPEndPoint DiscoveryMulticastEndpoint = new IPEndPoint(DiscoveryMulticastAddress, 49069);
		private static readonly TimeSpan DiscoveryMulticastInterval = TimeSpan.FromSeconds(1);
		private const int ConnectionPort = 49070;

		private readonly TcpListener m_connectionListener = new TcpListener(IPAddress.Any, ConnectionPort);
		private readonly UdpClient m_discoveryBroadcaster = new UdpClient();

		private CancellationTokenSource m_cancellationTokenSource = new CancellationTokenSource();

		private Task? m_discoveryBroadcastTask = null;

		public void Start()
		{
			m_discoveryBroadcastTask = new Task(BackgroundDiscoveryBroadcastTask);
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
					using (BinaryWriter writer = new BinaryWriter(ms, Encoding.ASCII))
					{
						CameraControlTransport.Write(new CameraControlDiscoveryPacket((IPEndPoint)m_connectionListener.LocalEndpoint, ConnectionPort), writer);
					}
					m_discoveryBroadcaster.Send(new ReadOnlySpan<byte>(buffer, 0, (int)ms.Position), DiscoveryMulticastEndpoint);
				}
			}
		}
	}
}