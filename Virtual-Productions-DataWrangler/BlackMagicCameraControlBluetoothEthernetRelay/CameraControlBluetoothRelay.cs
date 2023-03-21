using System.Collections.Concurrent;
using BlackmagicCameraControlBluetooth;
using BlackmagicCameraControlData;
using BlackmagicCameraControlData.CommandPackets;
using CameraControlOverEthernet;
using DataWranglerCommon;

namespace BlackMagicCameraControlBluetoothEthernetRelay
{
	internal class CameraControlBluetoothRelay
	{
		class CommandPacketCacheEntry
		{
			public TimeCode ReceivedTime;
			public byte[] RawCommandBytes;

			public CommandPacketCacheEntry(TimeCode a_receivedTime, byte[] a_rawCommandBytes)
			{
				ReceivedTime = a_receivedTime;
				RawCommandBytes = a_rawCommandBytes;
			}
		};


		private BlockingCollection<Action> m_callbackQueue = new BlockingCollection<Action>();
		private CameraControlNetworkTransmitter m_networkTransmitter;

		private BlackmagicBluetoothCameraAPIController m_cameraApiController = new BlackmagicBluetoothCameraAPIController();
		private Dictionary<CameraDeviceHandle, Dictionary<CommandIdentifier, CommandPacketCacheEntry>> m_mostRecentValuesByCamera = new();

		public bool ShouldKeepRunning => true;
		
		public CameraControlBluetoothRelay()
		{
			m_networkTransmitter = new CameraControlNetworkTransmitter();
			m_networkTransmitter.OnConnected += (a_serverId) => m_callbackQueue.Add(() => { OnConnectedToServer(a_serverId); });
			m_networkTransmitter.StartListenForServer();

			m_cameraApiController.OnCameraConnected += OnCameraConnected;
			m_cameraApiController.OnCameraDisconnected += OnCameraDisconnected;
			m_cameraApiController.OnRawBluetoothCommandDataReceived += OnRawBluetoothDataReceived;
			m_cameraApiController.OnTimeCodeReceived += OnTimeCodeReceived;
			m_cameraApiController.Start();
		}

		private void OnConnectedToServer(int a_serverId)
		{
			foreach(string deviceUuid in m_cameraApiController.GetConnectedCameraUuids())
			{
				CameraDeviceHandle deviceHandle = new CameraDeviceHandle(deviceUuid, m_cameraApiController);
				m_networkTransmitter.SendPacket(new CameraControlCameraConnectedPacket(deviceHandle));

				if (m_mostRecentValuesByCamera.TryGetValue(deviceHandle, out var cache))
				{
					foreach (var kvp in cache)
					{
						m_networkTransmitter.SendPacket(new CameraControlDataPacket(deviceHandle, kvp.Value.ReceivedTime, kvp.Value.RawCommandBytes));
					}
				}
			}
		}

		public void Update()
		{
			if (m_callbackQueue.TryTake(out Action? callbackAction, 1000))
			{
				callbackAction();
			}

			m_networkTransmitter.Update();
		}

		private void OnRawBluetoothDataReceived(CameraDeviceHandle a_deviceHandle, TimeCode a_receivedTime, byte[] a_payload)
		{
			using MemoryStream ms = new MemoryStream(a_payload, 0, a_payload.Length, false);
			CommandReader reader = new CommandReader(ms);
			PacketHeader packetHeader = reader.ReadPacketHeader();
			CommandHeader header = reader.ReadCommandHeader();
			if (m_mostRecentValuesByCamera.TryGetValue(a_deviceHandle, out var cache))
			{
				cache[header.CommandIdentifier] = new CommandPacketCacheEntry(a_receivedTime, a_payload);
			}

			m_networkTransmitter.SendPacket(new CameraControlDataPacket(a_deviceHandle, a_receivedTime, a_payload));
		}

		private void OnCameraDisconnected(CameraDeviceHandle a_deviceHandle)
		{
			m_networkTransmitter.SendPacket(new CameraControlCameraDisconnectedPacket(a_deviceHandle));
			m_mostRecentValuesByCamera.Remove(a_deviceHandle);
		}

		private void OnCameraConnected(CameraDeviceHandle a_deviceHandle)
		{
			m_mostRecentValuesByCamera.Add(a_deviceHandle, new Dictionary<CommandIdentifier, CommandPacketCacheEntry>());
			m_networkTransmitter.SendPacket(new CameraControlCameraConnectedPacket(a_deviceHandle));
		}

		private void OnTimeCodeReceived(CameraDeviceHandle a_deviceHandle, TimeCode a_timeCode)
		{
			m_networkTransmitter.SendPacket(new CameraControlTimeCodeChanged(a_deviceHandle, a_timeCode));
		}
	}
}
