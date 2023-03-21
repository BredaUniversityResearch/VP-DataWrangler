using BlackmagicCameraControlData;
using BlackmagicCameraControlData.CommandPackets;
using DataWranglerCommon;

namespace CameraControlOverEthernet
{
	public class EthernetRelayCameraController: CameraControllerBase
	{
		private CameraControlNetworkReceiver m_receiver = new CameraControlNetworkReceiver();

		public EthernetRelayCameraController()
		{
			m_receiver.Start();
		}

		public void BlockingProcessReceivedMessages(TimeSpan a_fromSeconds, CancellationToken a_token)
		{
			bool processedMessage = false;
			do
			{
				processedMessage = m_receiver.TryDequeueMessage(out ICameraControlPacket? packet, a_fromSeconds, a_token);
				if (processedMessage)
				{
					ProcessPacket(packet!);
				}
			} while (processedMessage);
		}

		private void ProcessPacket(ICameraControlPacket a_cameraControlPacket)
		{
			if (a_cameraControlPacket is CameraControlCameraConnectedPacket connectedPacket)
			{
				CameraConnected(new CameraDeviceHandle(connectedPacket.DeviceUuid, this));
			}
			else if (a_cameraControlPacket is CameraControlCameraDisconnectedPacket disconnectedPacket)
			{
				CameraDisconnected(new CameraDeviceHandle(disconnectedPacket.DeviceUuid, this));
			}
			else if (a_cameraControlPacket is CameraControlDataPacket dataPacket)
			{
				CameraDeviceHandle handle = new CameraDeviceHandle(dataPacket.DeviceUuid, this);
				TimeCode receivedTimeCode = TimeCode.FromBCD(dataPacket.ReceivedTimeCodeAsBCD);

				MemoryStream ms = new MemoryStream(dataPacket.PacketData);
				CommandReader.DecodeStream(ms, (_, a_packet) => { CameraDataReceived(handle, receivedTimeCode, a_packet);});
			}
			else if (a_cameraControlPacket is CameraControlTimeCodeChanged timeCodeChanged)
			{
				CameraDeviceHandle handle = new CameraDeviceHandle(timeCodeChanged.DeviceUuid, this);
				TimeCode receivedTimeCode = TimeCode.FromBCD(timeCodeChanged.TimeCodeAsBCD);
				CameraDataReceived(handle, receivedTimeCode, new CommandPacketSystemTimeCode() {BinaryCodedTimeCode = receivedTimeCode.TimeCodeAsBinaryCodedDecimal, TimeCode = receivedTimeCode});
			}
		}
	}
}
