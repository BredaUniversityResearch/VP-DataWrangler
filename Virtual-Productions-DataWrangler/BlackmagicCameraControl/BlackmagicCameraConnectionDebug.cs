using System;
using System.Collections.Generic;
using System.Threading;
using BlackmagicCameraControl.CommandPackets;

namespace BlackmagicCameraControl
{
	public class BlackmagicCameraConnectionDebug: IBlackmagicCameraConnection
	{
		public CameraHandle CameraHandle { get; }
		public DateTimeOffset LastReceivedDataTime { get; private set; }
		public IBlackmagicCameraConnection.EConnectionState ConnectionState => IBlackmagicCameraConnection.EConnectionState.Connected;
		public string DeviceId { get; }
		public string HumanReadableName { get; }
		private BlackmagicCameraAPIController m_dispatcher;

		private Thread m_messageProducerThread;
		private CancellationTokenSource m_messageProducerCancellationTokenSource = new CancellationTokenSource();

		private Queue<ICommandPacketBase> m_packetSendQueue = new Queue<ICommandPacketBase>();

		public BlackmagicCameraConnectionDebug(BlackmagicCameraAPIController a_dispatcher, CameraHandle a_handle)
		{
			m_dispatcher = a_dispatcher;

			CameraHandle = a_handle;
			DeviceId = "DEBUG_CONNECTION_"+a_handle.ConnectionId;
			HumanReadableName = "Debug Connection " + a_handle.ConnectionId;
			LastReceivedDataTime = DateTimeOffset.UtcNow;

			m_messageProducerThread = new Thread(BackgroundProduceMessages);
			m_messageProducerThread.Start();

			m_packetSendQueue.Enqueue(new CommandPacketMediaCodec()
				{BasicCodec = CommandPacketMediaCodec.EBasicCodec.BlackmagicRAW, Variant = 0});
			m_packetSendQueue.Enqueue(new CommandPacketVendorStorageTargetChanged() {StorageDriveIdentifier = 014 });
		}

		private void BackgroundProduceMessages()
		{
			//Add an initial delay so we have time for the connection process to be completed.
			m_messageProducerCancellationTokenSource.Token.WaitHandle.WaitOne(100);

			while (!m_messageProducerCancellationTokenSource.IsCancellationRequested)
			{
				m_dispatcher.NotifyDataReceived(CameraHandle, DateTimeOffset.UtcNow,
					new CommandPacketSystemBatteryInfo() {BatteryPercentage = 67, BatteryVoltage_mV = 1337});
				LastReceivedDataTime = DateTimeOffset.UtcNow;

				while (m_packetSendQueue.Count > 0)
				{
					ICommandPacketBase packet = m_packetSendQueue.Dequeue();
					m_dispatcher.NotifyDataReceived(CameraHandle, DateTimeOffset.UtcNow, packet);
				}

				m_messageProducerCancellationTokenSource.Token.WaitHandle.WaitOne(new TimeSpan(0, 0, 1));
			}
		}

		public void Dispose()
		{
			m_messageProducerCancellationTokenSource.Cancel();
			m_messageProducerThread.Join();
		}

		public void AsyncSendCommand(ICommandPacketBase a_command, ECommandOperation a_commandOperation)
		{
			if (a_command is CommandPacketMediaTransportMode)
			{
				m_packetSendQueue.Enqueue(a_command);
			}
		}
	}
}
