using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using BlackmagicCameraControl;
using BlackmagicCameraControl.CommandPackets;
using BlackmagicCameraControlData;
using CommonLogging;
using DataWranglerCommon;

namespace BlackmagicCameraControlBluetooth
{
	public class BlackmagicBluetoothCameraAPIController: CameraControllerBase, IDisposable
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

		public class AdvertisementEntry
		{
			public readonly string DeviceShortName;
			public readonly ulong DeviceBluetoothAddress;
			public readonly BluetoothAddressType DeviceBluetoothAddressType;
			public DateTimeOffset LastSeenTime;

			public AdvertisementEntry(string a_deviceShortName, ulong a_deviceBluetoothAddress, BluetoothAddressType a_deviceBluetoothAddressType)
			{
				DeviceShortName = a_deviceShortName;
				DeviceBluetoothAddress = a_deviceBluetoothAddress;
				DeviceBluetoothAddressType = a_deviceBluetoothAddressType;
				LastSeenTime = DateTimeOffset.UtcNow;
			}

			public void UpdateSeen()
			{
				LastSeenTime = DateTimeOffset.UtcNow;
			}
		};

		private static readonly TimeSpan BackgroundProcessingUpdateRate = new TimeSpan(0, 0, 1);
		private static readonly TimeSpan CameraReconnectTime = new TimeSpan(0, 0, 5);
		private static readonly TimeSpan BluetoothDeviceConnectTimeout = new TimeSpan(0, 0, 10);
		private static readonly TimeSpan CameraConnectSetupTimeout = new TimeSpan(0, 1, 15);
		private static readonly TimeSpan CameraDataReceivedTimeout = new TimeSpan(0, 0, 30);

		private const string DebugCameraDeviceUuid = "DEBUG_";

		public delegate string? CameraPairRequestPairingCode(string a_cameraDisplayName);

		private DeviceWatcher BLEDeviceWatcher = DeviceInformation.CreateWatcher(BluetoothLEDevice.GetDeviceSelector());
		private BluetoothLEAdvertisementWatcher m_bluetoothAdvertisementWatcher = new BluetoothLEAdvertisementWatcher();
		private Dictionary<ulong, AdvertisementEntry> m_availableDeviceAdvertisementsByBluetoothAddress = new();

		private readonly List<IBlackmagicCameraConnection> m_activeConnections = new List<IBlackmagicCameraConnection>();
		private readonly List<RetryEntry> m_retryConnectionQueue = new List<RetryEntry>();
		private readonly HashSet<string> m_activeConnectingSet = new HashSet<string>();

		public int ConnectedCameraCount => m_activeConnections.Count;
		public CameraPairRequestPairingCode? OnCameraRequestPairingCode = null;

		private Thread m_reconnectThread;
		private CancellationTokenSource m_backgroundProcessingCancellationToken;

		public BlackmagicBluetoothCameraAPIController()
		{
			BLEDeviceWatcher.Added += OnDeviceAdded;
			BLEDeviceWatcher.Removed += OnDeviceRemoved;
			BLEDeviceWatcher.Start();

			m_bluetoothAdvertisementWatcher.ScanningMode = BluetoothLEScanningMode.Active;
			m_bluetoothAdvertisementWatcher.Received += OnDeviceAdvertisementReceived;
            try
            {
                m_bluetoothAdvertisementWatcher.Start();
            }
            catch (COMException ex)
            {
                BlackmagicCameraLogInterface.LogError($"Failed to start Bluetooth advertisement watcher. Exception: {ex.Message}");
            }

            m_backgroundProcessingCancellationToken = new CancellationTokenSource();
			m_reconnectThread = new Thread(BackgroundProcessingMain);
			m_reconnectThread.Start();
		}

		public void Dispose()
		{
			m_backgroundProcessingCancellationToken.Cancel();

			foreach (IBlackmagicCameraConnection connection in m_activeConnections)
			{
				connection.Dispose();
			}

			m_activeConnections.Clear();

			//BLEDeviceWatcher.Stop();
			m_bluetoothAdvertisementWatcher.Stop();
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
					IBlackmagicCameraConnection connection = m_activeConnections[i];
					TimeSpan timeSinceLastDataReceived = DateTimeOffset.UtcNow - connection.LastReceivedDataTime;
					if (timeSinceLastDataReceived > CameraDataReceivedTimeout || connection.ConnectionState == IBlackmagicCameraConnection.EConnectionState.Disconnected)
					{
						CameraConnected(connection.CameraDeviceHandle);
						BlackmagicCameraLogInterface.LogInfo($"Camera \"{connection.HumanReadableName}\" ({connection.DeviceId}) disconnected due to data received timeout");

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
						
						AsyncTryConnectToDevice(retryEntry.DeviceId);
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
			AsyncTryConnectToDevice(a_args.Id);
		}

		private void OnDeviceRemoved(DeviceWatcher a_sender, DeviceInformationUpdate a_args)
		{
			for (int i = m_activeConnections.Count - 1; i >= 0; ++i)
			{
				if (m_activeConnections[i].DeviceId == a_args.Id)
				{
					CameraDisconnected(m_activeConnections[i].CameraDeviceHandle);
					m_activeConnections.RemoveAt(i);
				}
			}
		}

		public CameraDeviceHandle GetConnectedCameraByIndex(int a_index)
		{
			return m_activeConnections[a_index].CameraDeviceHandle;
		}

		public void AsyncTryConnectToDevice(string a_deviceAddress)
		{
			Task.Run(async () =>
			{
				lock (m_activeConnectingSet)
				{
					if (!m_activeConnectingSet.Contains(a_deviceAddress))
					{
						m_activeConnectingSet.Add(a_deviceAddress);
					}
				}

				BlackmagicCameraLogInterface.LogVerbose($"Trying to connect to Bluetooth device {a_deviceAddress}.");

				Task<BluetoothLEDevice> connectAttempt = BluetoothLEDevice.FromIdAsync(a_deviceAddress).AsTask();
				//TODO: Filter out useless device like HIDs via the use of the Device Appearance Category / Subcategory. 
				// -> connectAttempt.Result.Appearance.Category

				if (!connectAttempt.Wait(BluetoothDeviceConnectTimeout))
				{
					BlackmagicCameraLogInterface.LogVerbose($"Bluetooth device {a_deviceAddress} failed to connect.");
					m_retryConnectionQueue.Add(new RetryEntry(a_deviceAddress));
				}
				else
				{
					bool shouldRetry = true;
					string failReason = "";

					GattDeviceService? blackmagicService = null;
					GattDeviceService? deviceInformationService = null;

					BluetoothLEDevice connectedDevice = connectAttempt.Result;
					if (await TryPairWithDevice(connectedDevice))
					{
						GattDeviceServicesResult services =
							await connectedDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);
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
						else
						{
							if (connectedDevice.Appearance.Category != 0x0210)
							{
								failReason = $"Device ({connectedDevice.Name}) unreachable, and has incompatible category ({connectedDevice.Appearance.Category}). Expecting {0x0210})";
								shouldRetry = false;
							}
							else
							{
								failReason = "Device Unreachable";
							}
						}

						if (blackmagicService != null && deviceInformationService != null &&
						    connectedDevice.ConnectionStatus == BluetoothConnectionStatus.Connected) //Only check connection here as GetGattServicesAsync might still succeed if we don't have a connection...
						{
							BlackmagicCameraConnectionBluetooth deviceConnection =
								new BlackmagicCameraConnectionBluetooth(this,
									new CameraDeviceHandle(connectedDevice.DeviceId, this), connectedDevice, deviceInformationService,
									blackmagicService);
							if (deviceConnection.WaitForConnection(CameraConnectSetupTimeout) &&
							    deviceConnection.ConnectionState ==
							    IBlackmagicCameraConnection.EConnectionState.Connected)
							{
								m_activeConnections.Add(deviceConnection);
								CameraConnected(deviceConnection.CameraDeviceHandle);
								shouldRetry = false;

								BlackmagicCameraLogInterface.LogInfo($"Camera {connectedDevice.Name} Connected.");
							}
							else
							{
								failReason = "Device setup failed to complete in specified timeout";
							}
						}
						else
						{
							if (connectedDevice.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
							{
								failReason = "Device failed to connect";
							}
							else
							{
								failReason = "One or more bluetooth services could not be located";
							}
						}
					}
					else
					{
						BlackmagicCameraLogInterface.LogWarning($"Failed to pair with device {connectedDevice.Name}.");
					}

					if (shouldRetry &&
					    m_retryConnectionQueue.Find((a_obj) => a_obj.DeviceId == connectedDevice.DeviceId) == null)
					{
						BlackmagicCameraLogInterface.LogVerbose(
							$"Bluetooth Device {connectedDevice.Name} failed to connect: {failReason}. Retrying in a bit...");
						m_retryConnectionQueue.Add(new RetryEntry(connectedDevice.DeviceId));

						connectedDevice.Dispose();
					}

					lock (m_activeConnectingSet)
					{
						m_activeConnectingSet.Remove(a_deviceAddress);
					}
				}
			});
		}

		private async Task<bool> TryPairWithDevice(BluetoothLEDevice a_connectedDevice)
		{
			if (!a_connectedDevice.DeviceInformation.Pairing.IsPaired)
			{
				a_connectedDevice.DeviceInformation.Pairing.Custom.PairingRequested += OnDevicePairingRequested;
				DevicePairingResult pairResult =
					await a_connectedDevice.DeviceInformation.Pairing.Custom.PairAsync(DevicePairingKinds.ProvidePin);
				return (pairResult.Status == DevicePairingResultStatus.Paired);
			}

			return true;
		}

		private void OnDevicePairingRequested(DeviceInformationCustomPairing a_sender, DevicePairingRequestedEventArgs a_args)
		{
			if (a_args.PairingKind == DevicePairingKinds.ProvidePin)
			{
				string? pin = OnCameraRequestPairingCode?.Invoke(a_args.DeviceInformation.Name);
				if (!string.IsNullOrEmpty(pin))
				{
					a_args.Accept(pin);
				}
			}
			else
			{
				throw new NotImplementedException();
			}
		}

		private void OnDeviceAdvertisementReceived(BluetoothLEAdvertisementWatcher a_sender, BluetoothLEAdvertisementReceivedEventArgs a_args)
		{
			if (a_args.IsScanResponse)
			{
				if (!string.IsNullOrEmpty(a_args.Advertisement.LocalName))
				{
					if (m_availableDeviceAdvertisementsByBluetoothAddress.TryGetValue(a_args.BluetoothAddress, out var advertisementEntry))
					{
						advertisementEntry.UpdateSeen();
					}
					else
					{
						m_availableDeviceAdvertisementsByBluetoothAddress.Add(a_args.BluetoothAddress,
							new AdvertisementEntry(a_args.Advertisement.LocalName, a_args.BluetoothAddress,
								a_args.BluetoothAddressType));
					}
				}
			}
		}

		private IBlackmagicCameraConnection? FindCameraByHandle(CameraDeviceHandle a_cameraDeviceHandle)
		{
			foreach (IBlackmagicCameraConnection connection in m_activeConnections)
			{
				if (connection.CameraDeviceHandle == a_cameraDeviceHandle)
				{
					return connection;
				}
			}

			return null;
		}

		public void NotifyDataReceived(CameraDeviceHandle a_cameraDeviceHandle, TimeCode a_receivedTime, ICommandPacketBase a_packetInstance)
		{
			if (a_packetInstance is not CommandPacketSystemTimeCode &&
			    a_packetInstance is not CommandPacketSystemBatteryInfo)
			{
				BlackmagicCameraLogInterface.LogInfo($"Received BT Packet {a_packetInstance}");
			}

			CameraDataReceived(a_cameraDeviceHandle, a_receivedTime, a_packetInstance);
		}

		public void AsyncRequestCameraName(CameraDeviceHandle a_cameraDeviceHandle)
		{
			throw new NotImplementedException();
		}

		public void AsyncRequestCameraModel(CameraDeviceHandle a_cameraDeviceHandle)
		{
			IBlackmagicCameraConnection? connection = FindCameraByHandle(a_cameraDeviceHandle);
			if (connection is BlackmagicCameraConnectionBluetooth bluetoothConnection)
			{
				bluetoothConnection.AsyncRequestCameraModel().ContinueWith((a_result) =>
				{
					CameraDataReceived(a_cameraDeviceHandle, bluetoothConnection.LastReceivedTimeCode, new CommandPacketCameraModel(a_result.Result));
				});
			}
		}

		public void AsyncSendCommand(CameraDeviceHandle a_cameraDeviceHandle, ICommandPacketBase a_command, ECommandOperation a_commandOperation = ECommandOperation.Assign)
		{
			IBlackmagicCameraConnection? connection = FindCameraByHandle(a_cameraDeviceHandle);
			if (connection != null)
			{
				connection.AsyncSendCommand(a_command, a_commandOperation);
			}
			else
			{
				BlackmagicCameraLogInterface.LogWarning($"AsyncSendCommand failed: Camera deviceHandle {a_cameraDeviceHandle} was not found");
			}
		}

		public void CreateDebugCameraConnection()
		{
			BlackmagicCameraConnectionDebug debugConnection =
				new BlackmagicCameraConnectionDebug(this, new CameraDeviceHandle(DebugCameraDeviceUuid + Guid.NewGuid().ToString(), this));
			m_activeConnections.Add(debugConnection);
			CameraConnected(debugConnection.CameraDeviceHandle);
		}

		public IEnumerable<AdvertisementEntry> GetAdvertisedDevices()
		{
			return m_availableDeviceAdvertisementsByBluetoothAddress.Values;
		}
	}
}