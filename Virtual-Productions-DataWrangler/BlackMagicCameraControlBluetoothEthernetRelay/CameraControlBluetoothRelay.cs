using System.Collections.Concurrent;
using System.Diagnostics;
using BlackmagicCameraControlBluetooth;
using BlackmagicCameraControlData;
using BlackmagicCameraControlData.CommandPackets;
using CameraControlOverEthernet;
using DataWranglerCommon;

namespace BlackMagicCameraControlBluetoothEthernetRelay
{
	internal class CameraControlBluetoothRelay
	{
		private BlockingCollection<Action> m_callbackQueue = new BlockingCollection<Action>();
		private CameraControlNetworkClient m_networkClient;

		private BlackmagicBluetoothCameraAPIController m_cameraApiController = new BlackmagicBluetoothCameraAPIController();

		public bool ShouldKeepRunning => true;
		
		public CameraControlBluetoothRelay()
		{
			m_networkClient = new CameraControlNetworkClient();
			m_networkClient.OnConnected += (a_serverId) => m_callbackQueue.Add(() => { OnConnectedToServer(a_serverId); });
			m_networkClient.StartListenForServer();

			m_cameraApiController.OnCameraConnected += OnCameraConnected;
			m_cameraApiController.OnCameraDisconnected += OnCameraDisconnected;
			m_cameraApiController.OnCameraDataReceived += OnCameraDataReceived;
		}

		private void OnConnectedToServer(int a_serverId)
		{
		}

		public void Update()
		{
			if (m_callbackQueue.TryTake(out Action? callbackAction, 1000))
			{
				callbackAction();
			}

			m_networkClient.Update();
		}

		private void OnCameraDataReceived(CameraDeviceHandle a_deviceHandle, TimeCode a_receivedTime, ICommandPacketBase a_packet)
		{
			m_networkClient.SendPacket(new CameraControlDataPacket(a_deviceHandle, a_receivedTime, a_packet));
		}

		private void OnCameraDisconnected(CameraDeviceHandle a_deviceHandle)
		{
			throw new NotImplementedException();
		}

		private void OnCameraConnected(CameraDeviceHandle a_deviceHandle)
		{
			throw new NotImplementedException();
		}
	}
}
