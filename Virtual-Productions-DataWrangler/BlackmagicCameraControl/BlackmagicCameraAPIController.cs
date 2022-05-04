using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using BlackmagicCameraControl.CommandPackets;

namespace BlackmagicCameraControl
{
	public class BlackmagicCameraAPIController: IDisposable
	{
		private class RetryEntry
		{
			public string DeviceId;
			public DateTimeOffset LastConnectAttempt;

			public RetryEntry(string a_deviceId)
			{
				DeviceId = a_deviceId;
				LastConnectAttempt = DateTimeOffset.UtcNow;
			}
		};

		private static readonly TimeSpan BackgroundProcessingUpdateRate = new TimeSpan(0, 0, 1);
		private static readonly TimeSpan CameraReconnectTime = new TimeSpan(0, 0, 5);
		private static readonly TimeSpan ConnectTimeout = new TimeSpan(0, 0, 10);
		private static readonly TimeSpan CameraDataReceivedTimeout = new TimeSpan(0, 0, 15);

		public delegate void CameraConnectedDelegate(CameraHandle a_handle);
		public delegate void CameraDataReceivedDelegate(CameraHandle a_handle, DateTimeOffset a_receivedTime, ICommandPacketBase a_packet);

		private DeviceWatcher BLEDeviceWatcher = DeviceInformation.CreateWatcher(BluetoothLEDevice.GetDeviceSelectorFromPairingState(true));
		private int m_lastUsedHandle = 0;

		private List<BluetoothCameraConnection> m_activeConnections = new List<BluetoothCameraConnection>();
		private List<RetryEntry> m_retryConnectionQueue = new List<RetryEntry>();
		private HashSet<string> m_activeConnectingSet = new HashSet<string>();

		public int ConnectedCameraCount => m_activeConnections.Count;
		public event CameraConnectedDelegate OnCameraConnected = delegate { };
		public event CameraConnectedDelegate OnCameraDisconnected = delegate { };
		public event CameraDataReceivedDelegate OnCameraDataReceived = delegate { };

		private Thread m_reconnectThread;
		private CancellationTokenSource m_backgroundProcessingCancellationToken;

		public BlackmagicCameraAPIController()
		{
			BLEDeviceWatcher.Added += OnDeviceAdded;
			BLEDeviceWatcher.Removed += OnDeviceRemoved;
			BLEDeviceWatcher.Start();

			m_backgroundProcessingCancellationToken = new CancellationTokenSource();
			m_reconnectThread = new Thread(BackgroundProcessingMain);
			m_reconnectThread.Start();
		}

		public void Dispose()
		{
			m_backgroundProcessingCancellationToken.Cancel();

			foreach (BluetoothCameraConnection connection in m_activeConnections)
			{
				connection.Dispose();
			}

			m_activeConnections.Clear();

			BLEDeviceWatcher.Stop();
			m_reconnectThread.Join();
		}

		private void BackgroundProcessingMain()
		{
			while (!m_backgroundProcessingCancellationToken.IsCancellationRequested)
			{
				Stopwatch sw = new Stopwatch();
				sw.Start();

				for (int i = m_activeConnections.Count - 1; i >= 0; --i)
				{
					BluetoothCameraConnection connection = m_activeConnections[i];
					TimeSpan timeSinceLastDataReceived = DateTimeOffset.UtcNow - connection.LastReceivedDataTime;
					if (timeSinceLastDataReceived > CameraDataReceivedTimeout || connection.ConnectionState == BluetoothCameraConnection.EConnectionState.Disconnected)
					{
						OnCameraDisconnected(connection.CameraHandle);
						IBlackmagicCameraLogInterface.LogInfo($"Camera {connection.GetDevice().Name} disconnected due to data received timeout");

						m_retryConnectionQueue.Add(new RetryEntry(connection.DeviceId));

						m_activeConnections.RemoveAt(i);
						connection.Dispose();
					}
				}

				if (m_retryConnectionQueue.Count > 0)
				{
					TimeSpan timeSinceConnectAttempt = DateTimeOffset.UtcNow - m_retryConnectionQueue[0].LastConnectAttempt;
					if (timeSinceConnectAttempt > CameraReconnectTime)
					{
						RetryEntry retryEntry = m_retryConnectionQueue[0];
						m_retryConnectionQueue.RemoveAt(0);
						
						TryConnectToDevice(retryEntry.DeviceId);
					}
				}
				

				TimeSpan elapsedTime = sw.Elapsed;
				TimeSpan timeToSleep = BackgroundProcessingUpdateRate - elapsedTime;
				if (timeToSleep > TimeSpan.Zero)
				{
					m_backgroundProcessingCancellationToken.Token.WaitHandle.WaitOne(timeToSleep);
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
			Task.Run(async () =>
			{
				if (!m_activeConnectingSet.Contains(a_deviceId))
				{
					m_activeConnectingSet.Add(a_deviceId);
				}

				IBlackmagicCameraLogInterface.LogVerbose($"Trying to connect to Bluetooth device {a_deviceId}.");

				Task<BluetoothLEDevice> connectAttempt = BluetoothLEDevice.FromIdAsync(a_deviceId).AsTask();
				if (!connectAttempt.Wait(ConnectTimeout))
				{
					IBlackmagicCameraLogInterface.LogVerbose($"Bluetooth device {a_deviceId} failed to connect.");
					m_retryConnectionQueue.Add(new RetryEntry(a_deviceId));
				}
				else
				{
					bool shouldRetry = true;

					GattDeviceService? blackmagicService = null;
					GattDeviceService? deviceInformationService = null;

					BluetoothLEDevice connectedDevice = connectAttempt.Result;
					GattDeviceServicesResult services = await connectedDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);
					if (services.Status == GattCommunicationStatus.Success)
					{
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
					}

					if (blackmagicService != null && deviceInformationService != null && 
					    connectedDevice.ConnectionStatus == BluetoothConnectionStatus.Connected) //Only check connection here as GetGattServicesAsync might still succeed if we don't have a connection...
					{
						BluetoothCameraConnection deviceConnection = new BluetoothCameraConnection(this,
							new CameraHandle(++m_lastUsedHandle), connectedDevice, deviceInformationService, blackmagicService);
						if (deviceConnection.WaitForConnection(ConnectTimeout) &&
						    deviceConnection.ConnectionState == BluetoothCameraConnection.EConnectionState.Connected)
						{
							m_activeConnections.Add(deviceConnection);
							OnCameraConnected.Invoke(deviceConnection.CameraHandle);
							shouldRetry = false;

							IBlackmagicCameraLogInterface.LogInfo($"Camera {connectedDevice.Name} Connected.");
						}
					}

					if (shouldRetry &&
					    m_retryConnectionQueue.Find((a_obj) => a_obj.DeviceId == connectedDevice.DeviceId) == null)
					{
						IBlackmagicCameraLogInterface.LogVerbose(
							$"Bluetooth Device {connectedDevice.Name} failed to connect. Retrying in a bit...");
						m_retryConnectionQueue.Add(new RetryEntry(connectedDevice.DeviceId));

						connectedDevice.Dispose();
					}

					m_activeConnectingSet.Remove(a_deviceId);
				}
			});
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

		public void NotifyDataReceived(CameraHandle a_cameraHandle, DateTimeOffset a_receivedTime, ICommandPacketBase a_packetInstance)
		{
			OnCameraDataReceived.Invoke(a_cameraHandle, a_receivedTime, a_packetInstance);
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
					OnCameraDataReceived.Invoke(a_cameraHandle, DateTimeOffset.UtcNow, new CommandPacketCameraModel(a_result.Result));
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
			else
			{
				IBlackmagicCameraLogInterface.LogWarning($"AsyncSendCommand failed: Camera handle {a_cameraHandle} was not found");
			}
		}
	}
}