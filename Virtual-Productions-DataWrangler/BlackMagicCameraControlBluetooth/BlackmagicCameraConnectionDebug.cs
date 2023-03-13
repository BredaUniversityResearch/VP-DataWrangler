using System;
using System.Collections.Generic;
using System.Threading;
using BlackmagicCameraControlBluetooth;
using BlackmagicCameraControlData;
using BlackmagicCameraControlData.CommandPackets;
using DataWranglerCommon;

namespace BlackmagicCameraControl
{
	public class BlackmagicCameraConnectionDebug: IBlackmagicCameraConnection
	{
		private static int DebugDeviceId = 0;

		public CameraDeviceHandle CameraDeviceHandle { get; }
		public DateTimeOffset LastReceivedDataTime { get; private set; }
		public IBlackmagicCameraConnection.EConnectionState ConnectionState => IBlackmagicCameraConnection.EConnectionState.Connected;
		public string DeviceId { get; }
		public string HumanReadableName { get; }
		private BlackmagicBluetoothCameraAPIController m_dispatcher;

		private Thread m_messageProducerThread;
		private CancellationTokenSource m_messageProducerCancellationTokenSource = new CancellationTokenSource();

		private Queue<ICommandPacketBase> m_packetSendQueue = new Queue<ICommandPacketBase>();

		public BlackmagicCameraConnectionDebug(BlackmagicBluetoothCameraAPIController a_dispatcher, CameraDeviceHandle a_deviceHandle)
		{
			++DebugDeviceId;

			m_dispatcher = a_dispatcher;
			CameraDeviceHandle = a_deviceHandle;
			DeviceId = "DEBUG_CONNECTION_"+ DebugDeviceId;
			HumanReadableName = "Debug Connection " + DebugDeviceId;
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
				DateTime timeNow = DateTime.UtcNow;
				m_dispatcher.NotifyDataReceived(CameraDeviceHandle, new TimeCode(timeNow.Hour, timeNow.Minute, timeNow.Second, timeNow.Millisecond),
					new CommandPacketSystemBatteryInfo() {BatteryPercentage = 69, BatteryVoltage_mV = 1337});
				LastReceivedDataTime = DateTimeOffset.UtcNow;

				while (m_packetSendQueue.Count > 0)
				{
					ICommandPacketBase packet = m_packetSendQueue.Dequeue();
					m_dispatcher.NotifyDataReceived(CameraDeviceHandle, new TimeCode(timeNow.Hour, timeNow.Minute, timeNow.Second, timeNow.Millisecond), packet);
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
