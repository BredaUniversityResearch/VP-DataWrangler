﻿using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml;
using CommonLogging;

namespace CameraControlOverEthernet
{
	public class CameraControlNetworkServer
	{
		private class ClientConnection
		{
			public readonly TcpClient Client;
			public Task? ReceiveTask;
			public DateTime LastActivityTime;

			public ClientConnection(TcpClient a_client)
			{
				Client = a_client;
			}
		};

		public static readonly IPAddress DiscoveryMulticastAddress = IPAddress.Parse("224.0.0.69");
		public const int DiscoveryMulticastPort = 49069;
		public static readonly IPEndPoint DiscoveryMulticastEndpoint = new IPEndPoint(DiscoveryMulticastAddress, DiscoveryMulticastPort);
		public static readonly TimeSpan DiscoveryMulticastInterval = TimeSpan.FromSeconds(5);
		public const int ConnectionPort = 49070;
		public static readonly TimeSpan InactivityDisconnectTime = TimeSpan.FromSeconds(15);

		private readonly TcpListener m_connectionListener = new TcpListener(IPAddress.Any, ConnectionPort);
		private readonly UdpClient m_discoveryBroadcaster = new UdpClient();

		private CancellationTokenSource m_cancellationTokenSource = new CancellationTokenSource();

		private Task? m_discoveryBroadcastTask = null;
		private Task? m_connectAcceptTask = null;
		private readonly int m_serverIdentifier = Random.Shared.Next();

		private List<ClientConnection> m_connectedClients = new List<ClientConnection>();

		public void Start()
		{
			m_discoveryBroadcastTask = new Task(BackgroundDiscoveryBroadcastTask);
			m_discoveryBroadcastTask.Start();

			m_connectionListener.Start();

			m_connectAcceptTask = new Task(BackgroundAcceptConnections);
			m_connectAcceptTask.Start();
			Logger.LogVerbose("CCServer", $"Starting Camera Control Server. Using Multicast endpoint {DiscoveryMulticastEndpoint}. Listening on {m_connectionListener.LocalEndpoint}");
		}

		private async void BackgroundDiscoveryBroadcastTask()
		{
			byte[] buffer = new byte[128];
			while (!m_cancellationTokenSource.IsCancellationRequested)
			{
				MemoryStream ms = new MemoryStream(buffer, 0, buffer.Length, true, false);
				using (BinaryWriter writer = new BinaryWriter(ms, Encoding.ASCII, true))
				{
					CameraControlTransport.Write(new CameraControlDiscoveryPacket(m_serverIdentifier, ConnectionPort), writer);
				}
				m_discoveryBroadcaster.Send(new ReadOnlySpan<byte>(buffer, 0, (int)ms.Position), DiscoveryMulticastEndpoint);

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
					ClientConnection conn = new ClientConnection(client);
					conn.ReceiveTask = new Task(() => BackgroundReceiveData(conn));
					conn.ReceiveTask.Start();
					conn.LastActivityTime = DateTime.UtcNow;

					lock (m_connectedClients)
					{
						m_connectedClients.Add(conn);
					}
				}
			}
		}

		private async void BackgroundReceiveData(ClientConnection a_client)
		{
			byte[] receiveBuffer = new byte[8192];
			while (a_client.Client.Connected)
			{
				if (DateTime.UtcNow - a_client.LastActivityTime > InactivityDisconnectTime)
				{
					Logger.LogVerbose("CCServer", $"Dropping client at {a_client.Client.Client.RemoteEndPoint} due to inactivity");
					a_client.Client.Close();
					break;
				}

				int bytesReceived = await a_client.Client.GetStream().ReadAsync(receiveBuffer, 0, (int)receiveBuffer.Length);
				if (bytesReceived > 0)
				{
					MemoryStream ms = new MemoryStream(receiveBuffer, 0, bytesReceived, false);
					using (BinaryReader reader = new BinaryReader(ms, Encoding.ASCII))
					{
						while (ms.Position < ms.Length)
						{
							ICameraControlPacket? packet = CameraControlTransport.TryRead(reader);
							if (packet != null)
							{
								Logger.LogVerbose("CCServer", $"Received Packet From Client {a_client.Client.Client.RemoteEndPoint} of type {packet.GetType()}");
								a_client.LastActivityTime = DateTime.UtcNow;
							}
						}
					}
				}
			}
		}
	}
}