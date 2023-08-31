using BlackmagicCameraControlData;
using BlackmagicCameraControlData.CommandPackets;
using DataWranglerCommon;

namespace CameraControlOverEthernet
{
	public class EthernetRelayCameraController: CameraControllerBase
	{
		private CameraControlNetworkReceiver m_receiver = new CameraControlNetworkReceiver();

		private Dictionary<int, List<CameraDeviceHandle>> m_cameraHandlesByConnectionId = new();

		public EthernetRelayCameraController()
		{
			m_receiver.OnClientDisconnected += OnClientDisconnected;

			m_receiver.Start();
		}

		private void OnClientDisconnected(int a_connectionId)
		{
			if (m_cameraHandlesByConnectionId.TryGetValue(a_connectionId, out var deviceHandles))
			{
				foreach(CameraDeviceHandle handle in deviceHandles)
				{
					CameraDisconnected(handle);
				}

				m_cameraHandlesByConnectionId.Remove(a_connectionId);
			}
		}

		public void BlockingProcessReceivedMessages(TimeSpan a_fromSeconds, CancellationToken a_token)
		{
			bool processedMessage = false;
			do
			{
				processedMessage = m_receiver.TryDequeueMessage(out ICameraControlPacket? packet, out int a_connectionId, a_fromSeconds, a_token);
				if (processedMessage)
				{
					ProcessPacket(packet!, a_connectionId);
				}
			} while (processedMessage);
		}

		private void ProcessPacket(ICameraControlPacket a_cameraControlPacket, int a_connectionId)
		{
			if (a_cameraControlPacket is CameraControlCameraConnectedPacket connectedPacket)
			{
				CameraDeviceHandle handle = new CameraDeviceHandle(connectedPacket.DeviceUuid, this);
				if (!m_cameraHandlesByConnectionId.TryGetValue(a_connectionId, out List<CameraDeviceHandle>? handles))
				{
					handles = new List<CameraDeviceHandle>();
					m_cameraHandlesByConnectionId[a_connectionId] = handles;
				}

				handles.Add(handle);
				CameraConnected(handle);

				m_receiver.SendMessageToConnection(a_connectionId, new CameraControlRequestCurrentState(connectedPacket.DeviceUuid));
			}
			else if (a_cameraControlPacket is CameraControlCameraDisconnectedPacket disconnectedPacket)
			{
				CameraDeviceHandle handle = new CameraDeviceHandle(disconnectedPacket.DeviceUuid, this);
				if (m_cameraHandlesByConnectionId.TryGetValue(a_connectionId, out List<CameraDeviceHandle>? handles))
				{
					handles.RemoveAll((a_handle) => a_handle.DeviceUuid == handle.DeviceUuid);
					if (handles.Count == 0)
					{
						m_cameraHandlesByConnectionId.Remove(a_connectionId);
					}
				}

				CameraDisconnected(handle);
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

		public override bool TrySynchronizeClock(CameraDeviceHandle a_targetDevice, int a_minutesOffsetFromUtc, DateTime a_timeSyncPoint)
		{
			int connectionId = -1;
			foreach (var kvp in m_cameraHandlesByConnectionId)
			{
				if (kvp.Value.Contains(a_targetDevice))
				{
					connectionId = kvp.Key;
					break;
				}
			}

			if (connectionId != -1)
			{
				uint timeBcd = BinaryCodedDecimal.FromTime(a_timeSyncPoint);
				uint dateBcd = BinaryCodedDecimal.FromDate(a_timeSyncPoint);

				m_receiver.SendMessageToConnection(connectionId, new CameraControlConfigurationTimePacket((short)a_minutesOffsetFromUtc, timeBcd, dateBcd));
				return true;
			}

			return false;
		}
	}
}
