using BlackmagicCameraControlData;
using BlackmagicCameraControlData.CommandPackets;
using CommonLogging;
using DataWranglerCommon;

namespace CameraControlOverEthernet.CameraControl
{
    public class EthernetRelayCameraController: CameraControllerBase, INetworkAPIEventHandler
	{
		private NetworkedDeviceAPIServer m_apiServer;

		private Dictionary<int, List<CameraDeviceHandle>> m_cameraHandlesByConnectionId = new();

		public EthernetRelayCameraController(NetworkedDeviceAPIServer a_sourceServer)
		{
			m_apiServer = a_sourceServer;
			m_apiServer.RegisterEventHandler(this);

			m_apiServer.SendMessageToAllConnectedClients(new CameraControlRequestAllConnectedCameras());
		}

		public void OnClientConnected(int a_connectionId, NetworkAPIDeviceCapabilities a_deviceCapabilities)
		{
		}

		public void OnClientDisconnected(int a_connectionId)
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

		public void OnPacketReceived(INetworkAPIPacket a_packet, int a_connectionId)
		{
			if (a_packet is CameraControlCameraConnectedPacket connectedPacket)
			{
				CameraDeviceHandle handle = new CameraDeviceHandle(connectedPacket.DeviceUuid, this);
				if (!m_cameraHandlesByConnectionId.TryGetValue(a_connectionId, out List<CameraDeviceHandle>? handles))
				{
					handles = new List<CameraDeviceHandle>();
					m_cameraHandlesByConnectionId[a_connectionId] = handles;
				}

				handles.Add(handle);
				CameraConnected(handle);

				m_apiServer.SendMessageToConnection(a_connectionId, new CameraControlRequestCurrentState(connectedPacket.DeviceUuid));
			}
			else if (a_packet is CameraControlCameraDisconnectedPacket disconnectedPacket)
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
			else if (a_packet is CameraControlDataPacket dataPacket)
			{
				CameraDeviceHandle handle = new CameraDeviceHandle(dataPacket.DeviceUuid, this);
				TimeCode receivedTimeCode = TimeCode.FromBCD(dataPacket.ReceivedTimeCodeAsBCD);

				MemoryStream ms = new MemoryStream(dataPacket.PacketData);
				CommandReader.DecodeStream(ms, (_, a_packet) => { CameraDataReceived(handle, receivedTimeCode, a_packet);});
			}
			else if (a_packet is CameraControlTimeCodeChanged timeCodeChanged)
			{
				CameraDeviceHandle handle = new CameraDeviceHandle(timeCodeChanged.DeviceUuid, this);
				if (!m_cameraHandlesByConnectionId.TryGetValue(a_connectionId, out List<CameraDeviceHandle>? camerasForThisConnection))
				{
					m_apiServer.SendMessageToConnection(a_connectionId, new CameraControlRequestAllConnectedCameras());
					Logger.LogWarning("EthernetRelay", $"Received camera packet from handle that wasn't known, discarding packet of type {timeCodeChanged.GetType()} and requesting all active cameras");
					return;
				}

				if (!camerasForThisConnection.Contains(handle))
				{
					m_apiServer.SendMessageToConnection(a_connectionId, new CameraControlRequestAllConnectedCameras());
					Logger.LogWarning("EthernetRelay", $"Received camera packet from handle that wasn't known, discarding packet of type {timeCodeChanged.GetType()} and requesting all active cameras");
					return;
				}

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

				m_apiServer.SendMessageToConnection(connectionId, new CameraControlConfigurationTimePacket((short)a_minutesOffsetFromUtc, timeBcd, dateBcd));
				return true;
			}

			return false;
		}

		public void ReReportAllCameras()
		{
			foreach(var cameraHandlesByConnection in m_cameraHandlesByConnectionId)
			{
				foreach (var cameraHandle in cameraHandlesByConnection.Value)
				{
					CameraConnected(cameraHandle);
				}
			}
		}
	}
}
