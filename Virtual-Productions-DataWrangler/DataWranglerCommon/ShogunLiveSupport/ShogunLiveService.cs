using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace DataWranglerCommon.ShogunLiveSupport
{
	public class ShogunLiveService : IDisposable
	{
		private const int MessageIdHistory = 16;
		private static readonly Regex PacketIdPattern = new Regex("<PacketID VALUE=\"([0-9]+)\"/>");
		private static readonly Regex CaptureNamePattern = new Regex("<Name VALUE=\"([^\"]+)\"/>");
		private static readonly TimeSpan DefaultConfirmationTimeout = TimeSpan.FromSeconds(10);

		class ShogunLivePacket
		{
			public enum EType
			{
				CaptureStart,
				CaptureStop
			};

			public readonly EType PacketType;
			public readonly string CaptureName;

			public ShogunLivePacket(EType a_packetType, string a_captureName)
			{
				PacketType = a_packetType;
				CaptureName = a_captureName;
			}

		};

		class ConfirmationWaitEntry
		{
			private ManualResetEvent m_resetEvent = new ManualResetEvent(false);
			public Task<bool> ResultPromise;

			private ShogunLivePacket.EType m_expectedPacketType;
			private string m_expectedCaptureName;
			private DateTime m_timeOutTime;

			public ConfirmationWaitEntry(ShogunLivePacket.EType a_packetType, string a_name, TimeSpan a_timeout)
			{
				m_timeOutTime = DateTime.UtcNow + a_timeout;
				m_expectedPacketType = a_packetType;
				m_expectedCaptureName = a_name;
				ResultPromise = Task.Run(() => m_resetEvent.WaitOne(a_timeout));
			}

			public bool OnPacketReceived(ShogunLivePacket.EType a_packetType, string a_captureName)
			{
				if (DateTime.UtcNow > m_timeOutTime)
				{
					return true;
				}

				if (m_expectedPacketType == a_packetType && m_expectedCaptureName == a_captureName)
				{
					m_resetEvent.Set();
					return true;
				}

				return false;

			}
		};

		private UdpClient m_controlClient;
		private IPEndPoint m_targetEndPoint;

		private Task m_receiveTask;
		private CancellationTokenSource m_receiveCancellationTokenSource = new CancellationTokenSource();
		private List<ConfirmationWaitEntry> m_awaitConfirmationEntries = new List<ConfirmationWaitEntry>();
		private ConcurrentQueue<int> m_transmittedMessageIds = new ConcurrentQueue<int>();

		public ShogunLiveService(int a_controlClientPort)
		{
			m_controlClient = new UdpClient();
			m_controlClient.EnableBroadcast = true;
			m_controlClient.ExclusiveAddressUse = false;
			m_controlClient.Client.Bind(new IPEndPoint(IPAddress.Any, a_controlClientPort));

			/*NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
			foreach (NetworkInterface adapter in nics)
			{
				IPInterfaceProperties ipProps = adapter.GetIPProperties();
				if (ipProps.UnicastAddresses.Count > 0 &&
				    adapter.OperationalStatus == OperationalStatus.Up &&
				    adapter.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
				    adapter.Name.StartsWith("vEthernet") == false)
				{
					foreach (var unicastAddr in ipProps.UnicastAddresses)
					{
						//Double check that we have an IPv4 Address...
						if (unicastAddr.Address.AddressFamily == AddressFamily.InterNetwork)
						{
							break;
						}
					}

					if (m_controlClient != null)
					{
						break;
					}
				}
			}*/

			if (m_controlClient == null)
			{
				throw new Exception("Failed to find network adapter to bind Shogun Service to");
			}

			m_receiveTask = Task.Run(BackgroundListenForBroadcasts);

			m_targetEndPoint = new IPEndPoint(IPAddress.Broadcast, a_controlClientPort);
		}

		public void Dispose()
		{
			m_receiveCancellationTokenSource.Cancel();
			if (!m_receiveTask.Wait(500))
			{
				throw new Exception("Background receive task failed to terminate in reasonable time frame");
			}

			m_controlClient.Dispose();
		}

		public bool StartCapture(string a_captureName, string a_databasePath, [NotNullWhen(true)] out Task<bool>? a_confirmationResultTask, int a_startDelayMs = 0,
			string? a_notes = null,
			string? a_description = null)
		{
			int packetId = Random.Shared.Next();

			string packetContents = $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>" +
			                        $"<CaptureStart>" +
			                        $"  <Name VALUE=\"{a_captureName}\"/>" +
			                        $"  <Notes VALUE=\"{a_notes}\"/>" +
			                        $"  <Description VALUE=\"{a_description}\"/>" +
			                        $"  <DatabasePath VALUE=\"{a_databasePath}\"/>" +
			                        $"  <Delay VALUE=\"{a_startDelayMs}\"/>" +
			                        $"  <PacketID VALUE=\"{packetId}\"/>" +
			                        $"</CaptureStart>";
			a_confirmationResultTask = null;

			OnPreTransmitMessageWithId(packetId);
			bool successTransmit = BroadcastStringToShogunLive(packetContents);
			if (successTransmit)
			{
				a_confirmationResultTask = WaitForConfirmationPacket(ShogunLivePacket.EType.CaptureStart, a_captureName);
			}

			return successTransmit;
		}

		private Task<bool> WaitForConfirmationPacket(ShogunLivePacket.EType a_packetType, string a_captureName)
		{
			ConfirmationWaitEntry entry =
				new ConfirmationWaitEntry(a_packetType, a_captureName, DefaultConfirmationTimeout);
			lock (m_awaitConfirmationEntries)
			{
				m_awaitConfirmationEntries.Add(entry);
			}

			return entry.ResultPromise;
		}

		private void OnPreTransmitMessageWithId(int a_packetId)
		{
			m_transmittedMessageIds.Enqueue(a_packetId);
			while (m_transmittedMessageIds.Count > MessageIdHistory)
			{
				m_transmittedMessageIds.TryDequeue(out int _);
			}
		}

		public bool StopCapture(bool a_captureSuccess, string a_captureName, string a_databasePath,
			int a_stopDelayMs = 0)
		{
			int packetId = Random.Shared.Next();
			string resultText = (a_captureSuccess) ? "SUCCESS" : "CANCEL";
			string packetContents = $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\"?>" +
			                        $"<CaptureStop RESULT=\"{resultText}\">" +
			                        $"    <Name VALUE=\"{a_captureName}\"/>" +
			                        $"    <DatabasePath VALUE=\"{a_databasePath}\"/>" +
			                        $"    <Delay VALUE=\"{a_stopDelayMs}\"/>" +
			                        $"    <PacketID VALUE=\"{packetId}\"/>" +
			                        $"</CaptureStop>";
			OnPreTransmitMessageWithId(packetId);
			return BroadcastStringToShogunLive(packetContents);
		}

		private bool BroadcastStringToShogunLive(string a_xml)
		{
			byte[] packetData = Encoding.UTF8.GetBytes(a_xml);

			int sentBytes = m_controlClient.Send(packetData, m_targetEndPoint);
			return (sentBytes == packetData.Length);
		}

		private async void BackgroundListenForBroadcasts()
		{
			while (!m_receiveCancellationTokenSource.IsCancellationRequested)
			{
				UdpReceiveResult receivedData =
					await m_controlClient.ReceiveAsync(m_receiveCancellationTokenSource.Token);

				if (receivedData.Buffer.Length > 0 && !Equals(receivedData.RemoteEndPoint, m_controlClient.Client.LocalEndPoint))
				{
					string xmlFragment = Encoding.UTF8.GetString(receivedData.Buffer);
					Match packetIdMatch = PacketIdPattern.Match(xmlFragment);
					Match captureNameMatch = CaptureNamePattern.Match(xmlFragment);
					if (!packetIdMatch.Success || packetIdMatch.Groups.Count != 2 ||
					    !captureNameMatch.Success || captureNameMatch.Groups.Count != 2)
					{
						continue; //Malformed packet?
					}

					if (int.TryParse(packetIdMatch.Groups[1].ValueSpan, out int parsedPacketId))
					{
						if (m_transmittedMessageIds.Contains(parsedPacketId))
						{
							continue; //We transmitted this message, so lets just ignore it.
						}
					}

					if (xmlFragment.Contains("<CaptureStart"))
					{
						OnReceivedPacket(new ShogunLivePacket(ShogunLivePacket.EType.CaptureStart,
							captureNameMatch.Groups[1].Value));

					}
					else if (xmlFragment.Contains("<CaptureStop"))
					{
						OnReceivedPacket(new ShogunLivePacket(ShogunLivePacket.EType.CaptureStop,
							captureNameMatch.Groups[1].Value));
					}
					else if (xmlFragment.Contains("<CaptureComplete>"))
					{
						//Todo: Handle this 
					}
					else
					{
						throw new NotImplementedException($"Unknown message: {xmlFragment}");
					}
				}
			}
		}

		private void OnReceivedPacket(ShogunLivePacket a_packet)
		{
			lock (m_awaitConfirmationEntries)
			{
				for (int i = m_awaitConfirmationEntries.Count - 1; i >= 0; --i)
				{
					if (m_awaitConfirmationEntries[i].OnPacketReceived(a_packet.PacketType, a_packet.CaptureName))
					{
						m_awaitConfirmationEntries.RemoveAt(i);
					}
				}
			}
		}
	}
}