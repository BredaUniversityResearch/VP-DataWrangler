using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using BlackmagicCameraControl.CommandPackets;

namespace BlackmagicCameraControl
{
	public class BlackmagicCameraController: IDisposable
	{
		public delegate void CameraConnectedDelegate(CameraHandle a_handle);
		public delegate void CameraDataReceivedDelegate(CameraHandle a_handle, ICommandPacketBase a_packet);

		private DeviceWatcher BLEDeviceWatcher = DeviceInformation.CreateWatcher(BluetoothLEDevice.GetDeviceSelectorFromPairingState(true));
		private int m_LastUsedHandle = 0;

		private List<BluetoothCameraConnection> m_activeConnections = new List<BluetoothCameraConnection>();
		private List<string> m_retryConnectionQueue = new List<string>();
		private HashSet<string> m_activeConnectingSet = new HashSet<string>();

		public int ConnectedCameraCount => m_activeConnections.Count;
		public event CameraConnectedDelegate OnCameraConnected = delegate { };
		public event CameraDataReceivedDelegate OnCameraDataReceived = delegate { };

		public BlackmagicCameraController()
		{
			BLEDeviceWatcher.Added += OnDeviceAdded;
			BLEDeviceWatcher.Removed += OnDeviceRemoved;
			BLEDeviceWatcher.Start();
		}

		public void Dispose()
		{
			foreach (BluetoothCameraConnection connection in m_activeConnections)
			{
				connection.Dispose();
			}

			m_activeConnections.Clear();

			BLEDeviceWatcher.Stop();
		}

		private void OnDeviceAdded(DeviceWatcher a_sender, DeviceInformation a_args)
		{
			TryConnectToDevice(a_args.Id);
		}

		private void OnDeviceRemoved(DeviceWatcher a_sender, DeviceInformationUpdate a_args)
		{
			m_activeConnections.RemoveAll(a_connection => a_connection.DeviceId == a_args.Id);
		}

		public CameraHandle GetConnectedCameraByIndex(int a_index)
		{
			return m_activeConnections[a_index].CameraHandle;
		}

		private void TryConnectToDevice(string a_deviceId)
		{
			if (!m_activeConnectingSet.Contains(a_deviceId))
			{
				m_activeConnectingSet.Add(a_deviceId);
			}

			BluetoothLEDevice.FromIdAsync(a_deviceId).Completed =
				(a_connectedDeviceOp, _) =>
				{
					BluetoothLEDevice connectedDevice = a_connectedDeviceOp.GetResults();
					GattDeviceServicesResult services = connectedDevice.GetGattServicesAsync().GetResults();

					GattDeviceService? blackmagicService = null;
					GattDeviceService? deviceInformationService = null;

					foreach (GattDeviceService service in services.Services)
					{
						if (service.Uuid == BlackmagicCameraBluetoothGUID.BlackmagicServiceUUID)
						{
							blackmagicService = service;
						}
						else if (service.Uuid == BlackmagicCameraBluetoothGUID.DeviceInformationServiceUUID)
						{
							deviceInformationService = service;
						}
					}

					if (blackmagicService != null && deviceInformationService != null)
					{
						BluetoothCameraConnection deviceConnection = new BluetoothCameraConnection(this, new CameraHandle(++m_LastUsedHandle),
							connectedDevice, deviceInformationService, blackmagicService);
						deviceConnection.WaitForConnection(new TimeSpan(0, 0, 5));
						if (deviceConnection.ConnectionState == BluetoothCameraConnection.EConnectionState.Connected)
						{
							m_activeConnections.Add(deviceConnection);
							OnCameraConnected.Invoke(deviceConnection.CameraHandle);
						}
						else
						{
							m_retryConnectionQueue.Add(connectedDevice.DeviceId);
						}
						m_activeConnectingSet.Remove(a_deviceId);
					}
				};
		}

		public void NotifyDataReceived(CameraHandle a_cameraHandle, ICommandPacketBase a_packetInstance)
		{
			OnCameraDataReceived.Invoke(a_cameraHandle, a_packetInstance);
		}
	}
}