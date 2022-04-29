using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using BlackmagicCameraControl.CommandPackets;

namespace BlackmagicCameraControl
{
	public class BlackmagicCameraController: IDisposable
	{
		private static readonly TimeSpan ReconnectTimeout = new TimeSpan(0, 0, 5);
		private static readonly TimeSpan ConnectTimeout = new TimeSpan(0, 0, 15);

		public delegate void CameraConnectedDelegate(CameraHandle a_handle);
		public delegate void CameraDataReceivedDelegate(CameraHandle a_handle, ICommandPacketBase a_packet);

		private DeviceWatcher BLEDeviceWatcher = DeviceInformation.CreateWatcher(BluetoothLEDevice.GetDeviceSelectorFromPairingState(true));
		private int m_lastUsedHandle = 0;

		private List<BluetoothCameraConnection> m_activeConnections = new List<BluetoothCameraConnection>();
		private List<string> m_retryConnectionQueue = new List<string>();
		private HashSet<string> m_activeConnectingSet = new HashSet<string>();

		public int ConnectedCameraCount => m_activeConnections.Count;
		public event CameraConnectedDelegate OnCameraConnected = delegate { };
		public event CameraDataReceivedDelegate OnCameraDataReceived = delegate { };

		private Thread m_reconnectThread;
		private CancellationTokenSource m_reconnectThreadCancellationToken;

		public BlackmagicCameraController()
		{
			BLEDeviceWatcher.Added += OnDeviceAdded;
			BLEDeviceWatcher.Removed += OnDeviceRemoved;
			BLEDeviceWatcher.Start();

			m_reconnectThreadCancellationToken = new CancellationTokenSource();
			m_reconnectThread = new Thread(BackgroundReconnectTask);
			m_reconnectThread.Start();
		}

		public void Dispose()
		{
			m_reconnectThreadCancellationToken.Cancel();

			foreach (BluetoothCameraConnection connection in m_activeConnections)
			{
				connection.Dispose();
			}

			m_activeConnections.Clear();

			BLEDeviceWatcher.Stop();
			m_reconnectThread.Join();
		}

		private void BackgroundReconnectTask()
		{
			while (!m_reconnectThreadCancellationToken.IsCancellationRequested)
			{
				if (m_retryConnectionQueue.Count > 0)
				{
					string deviceId = m_retryConnectionQueue[0];
					m_retryConnectionQueue.RemoveAt(0);
					Stopwatch sw = new Stopwatch();
					sw.Start();
					TryConnectToDevice(deviceId);
					TimeSpan elapsedTime = sw.Elapsed;

					TimeSpan timeToSleep = ReconnectTimeout - elapsedTime;
					if (timeToSleep > TimeSpan.Zero)
					{
						m_reconnectThreadCancellationToken.Token.WaitHandle.WaitOne(timeToSleep);
					}
				}
				else
				{
					m_reconnectThreadCancellationToken.Token.WaitHandle.WaitOne(ReconnectTimeout);
				}
			}
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
						BluetoothCameraConnection deviceConnection = new BluetoothCameraConnection(this, new CameraHandle(++m_lastUsedHandle),
							connectedDevice, deviceInformationService, blackmagicService);
						if (deviceConnection.WaitForConnection(ConnectTimeout) && 
						    deviceConnection.ConnectionState == BluetoothCameraConnection.EConnectionState.Connected)
						{
							m_activeConnections.Add(deviceConnection);
							OnCameraConnected.Invoke(deviceConnection.CameraHandle);
						}
						else
						{
							if (!m_retryConnectionQueue.Contains(connectedDevice.DeviceId))
							{
								m_retryConnectionQueue.Add(connectedDevice.DeviceId);
							}
						}
						m_activeConnectingSet.Remove(a_deviceId);
					}
				};
		}

		private BluetoothCameraConnection? FindCameraByHandle(CameraHandle a_cameraHandle)
		{
			foreach (BluetoothCameraConnection connection in m_activeConnections)
			{
				if (connection.CameraHandle == a_cameraHandle)
				{
					return connection;
				}
			}

			return null;
		}

		public void NotifyDataReceived(CameraHandle a_cameraHandle, ICommandPacketBase a_packetInstance)
		{
			OnCameraDataReceived.Invoke(a_cameraHandle, a_packetInstance);
		}

		public void AsyncRequestCameraName(CameraHandle a_cameraHandle)
		{
			throw new NotImplementedException();
		}

		public void AsyncRequestCameraModel(CameraHandle a_cameraHandle)
		{
			BluetoothCameraConnection? connection = FindCameraByHandle(a_cameraHandle);
			if (connection != null)
			{
				connection.AsyncRequestCameraModel().ContinueWith((a_result) =>
				{
					OnCameraDataReceived.Invoke(a_cameraHandle, new CommandPacketCameraModel(a_result.Result));
				});
			}
		}

		public string GetBluetoothName(CameraHandle a_cameraHandle)
		{
			BluetoothCameraConnection? connection = FindCameraByHandle(a_cameraHandle);
			if (connection != null)
			{
				return connection.GetDevice().Name;
			}

			return "Invalid Camera Handle";
		}

		public void AsyncSendCommand(CameraHandle a_cameraHandle, ICommandPacketBase a_command)
		{
			BluetoothCameraConnection? connection = FindCameraByHandle(a_cameraHandle);
			if (connection != null)
			{
				connection.AsyncSendCommand(a_command);
			}
		}
	}
}