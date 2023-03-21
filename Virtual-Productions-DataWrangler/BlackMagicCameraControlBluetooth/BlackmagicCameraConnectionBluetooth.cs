using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Storage.Streams;
using BlackmagicCameraControlBluetooth;
using BlackmagicCameraControlData;
using BlackmagicCameraControlData.CommandPackets;
using DataWranglerCommon;

namespace BlackmagicCameraControl
{
	internal class BlackmagicCameraConnectionBluetooth : IBlackmagicCameraConnection
	{
		[Flags]
		public enum EBluetoothCameraStatus
		{
			None = 0x00,
			PowerOn = 0x01,
			Connected = 0x02,
			Paired = 0x04,
			VersionsVerified = 0x08,
			InitialPayloadReceived = 0x10,
			CameraReady = 0x20
		};

		public string DeviceId => m_device.DeviceId;

		public IBlackmagicCameraConnection.EConnectionState ConnectionState { get; private set; }
		public CameraDeviceHandle CameraDeviceHandle { get; private set; }
		public DateTimeOffset LastReceivedDataTime { get; private set; }
		public TimeCode LastReceivedTimeCode { get; private set; } = TimeCode.Invalid;
		public string HumanReadableName => GetDevice().Name;

		private bool m_isInInitialReset = false;
		private readonly BlackmagicBluetoothCameraAPIController m_dispatcher;

		private readonly BluetoothLEDevice m_device;
		private readonly GattDeviceService m_deviceInformationService;
		private GattCharacteristic? m_deviceInformationCameraManufacturer;
		private GattCharacteristic? m_deviceInformationCameraModel;
		private readonly GattDeviceService m_blackmagicService;
		private GattCharacteristic? m_blackmagicServiceOutgoingCameraControl;
		private GattCharacteristic? m_blackmagicServiceIncomingCameraControl;
		private GattCharacteristic? m_blackmagicServiceTimecode;
		private GattCharacteristic? m_blackmagicCameraStatus;

		private Task? m_connectingTask;

		public BlackmagicCameraConnectionBluetooth(BlackmagicBluetoothCameraAPIController a_dispatcher, CameraDeviceHandle a_cameraDeviceHandle, BluetoothLEDevice a_target, GattDeviceService a_deviceInformationService,
			GattDeviceService a_blackmagicService)
		{
			CameraDeviceHandle = a_cameraDeviceHandle;
			m_dispatcher = a_dispatcher;
			m_device = a_target;
			m_deviceInformationService = a_deviceInformationService;
			m_blackmagicService = a_blackmagicService;

			m_device.ConnectionStatusChanged += OnConnectionStatusChanged;

			m_connectingTask = Task.Run(() =>
			{
				BlackmagicCameraLogInterface.LogVerbose($"Querying device information service for {m_device.Name}");
				IReadOnlyList<GattCharacteristic> characteristics = m_deviceInformationService.GetAllCharacteristics();
				if (characteristics != null)
				{
					BlackmagicCameraLogInterface.LogVerbose($"Device information service query succeeded for {m_device.Name}");
					foreach (GattCharacteristic characteristic in characteristics)
					{
						if (characteristic.Uuid ==
							BlackmagicCameraBluetoothGUID.DeviceInformationService_CameraManufacturer)
						{
							m_deviceInformationCameraManufacturer = characteristic;
						}
						else if (characteristic.Uuid ==
								 BlackmagicCameraBluetoothGUID.DeviceInformationService_CameraModel)
						{
							m_deviceInformationCameraModel = characteristic;
						}
					}
					IReadOnlyList<GattCharacteristic>? blackmagicCharacteristics = null;
					try
					{
						blackmagicCharacteristics = m_blackmagicService.GetAllCharacteristics();
					}
					catch (FileLoadException)
					{
						BlackmagicCameraLogInterface.LogVerbose($"Failed to load characteristics for device {m_device.Name}. File descriptor in use. ");
					}

					if (blackmagicCharacteristics != null)
					{
						foreach (GattCharacteristic characteristic in blackmagicCharacteristics)
						{
							if (characteristic.Uuid ==
								BlackmagicCameraBluetoothGUID.BlackmagicService_IncomingCameraControl)
							{
								m_blackmagicServiceIncomingCameraControl = characteristic;
							}
							else if (characteristic.Uuid ==
									 BlackmagicCameraBluetoothGUID.BlackmagicService_OutgoingCameraControl)
							{
								m_blackmagicServiceOutgoingCameraControl = characteristic;
							}
							else if (characteristic.Uuid ==
							         BlackmagicCameraBluetoothGUID.BlackmagicService_Timecode)
							{
								m_blackmagicServiceTimecode = characteristic;
							}
							else if (characteristic.Uuid ==
							         BlackmagicCameraBluetoothGUID.BlackmagicService_CameraStatus)
							{
								m_blackmagicCameraStatus = characteristic;
							}
						}
					}
				}
				else
				{
					BlackmagicCameraLogInterface.LogVerbose($"Device information service query failed for {m_device.Name}");
				}

				if (m_deviceInformationCameraManufacturer != null && 
				    m_deviceInformationCameraModel != null &&
				    m_blackmagicServiceOutgoingCameraControl != null &&
				    m_blackmagicServiceIncomingCameraControl != null &&
				    m_blackmagicServiceTimecode != null &&
				    m_blackmagicCameraStatus != null)
				{
					SubscribeToIncomingServices();
					LastReceivedDataTime = DateTimeOffset.UtcNow;
					ConnectionState = IBlackmagicCameraConnection.EConnectionState.Connected;
				}
				else
				{
					ConnectionState = IBlackmagicCameraConnection.EConnectionState.Disconnected;
				}
			});	
		}

		public async Task AsyncPerformSoftReset(int a_offTimeMs = 2000)
		{
			if (m_blackmagicCameraStatus == null)
			{
				throw new Exception();
			}

			m_isInInitialReset = true;
			IBuffer sendBuffer = WindowsRuntimeBuffer.Create(new byte[]{ 0x00}, 0, (int)1, (int)1);
			GattCommunicationStatus result = await m_blackmagicCameraStatus.WriteValueAsync(sendBuffer);
			if (result != GattCommunicationStatus.Success)
			{
				BlackmagicCameraLogInterface.LogError($"Initial reset failure to turn camera {HumanReadableName} off");
			}

			await Task.Delay(a_offTimeMs);

			sendBuffer = WindowsRuntimeBuffer.Create(new byte[]{ 0x01}, 0, (int)1, (int)1);
			result = await m_blackmagicCameraStatus.WriteValueAsync(sendBuffer);
			if (result != GattCommunicationStatus.Success)
			{
				BlackmagicCameraLogInterface.LogError($"Initial reset failure to turn camera {HumanReadableName} back on");
			}
		}

		public void Dispose()
		{
			m_device.Dispose();
			m_deviceInformationService.Dispose();
			m_blackmagicService.Dispose();
			m_connectingTask?.Dispose();
		}	

		public bool WaitForConnection(TimeSpan a_timeout)
		{
			if (m_connectingTask != null)
			{
				return m_connectingTask.Wait(a_timeout);
			}

			return ConnectionState == IBlackmagicCameraConnection.EConnectionState.Connected;
		}

		private void SubscribeToIncomingServices()
		{
			GattClientCharacteristicConfigurationDescriptorValue newValue = GattClientCharacteristicConfigurationDescriptorValue.Indicate;

			Debug.Assert(m_blackmagicServiceIncomingCameraControl != null, nameof(m_blackmagicServiceIncomingCameraControl) + " != null");

			m_blackmagicServiceIncomingCameraControl.WriteClientCharacteristicConfigurationDescriptorAsync(newValue).Completed = (_, _) =>
			{
				m_blackmagicServiceIncomingCameraControl.ValueChanged += OnReceivedIncomingCameraControl;
			};

			Debug.Assert(m_blackmagicServiceOutgoingCameraControl != null, nameof(m_blackmagicServiceOutgoingCameraControl) + " != null");
			m_blackmagicServiceOutgoingCameraControl
					.WriteClientCharacteristicConfigurationDescriptorAsync(
						GattClientCharacteristicConfigurationDescriptorValue.Notify).Completed =
				(_, _) =>
				{
					m_blackmagicServiceOutgoingCameraControl.ValueChanged += OnReceivedOutgoingCameraControlResponse;
				};

			Debug.Assert(m_blackmagicCameraStatus!= null, nameof(m_blackmagicCameraStatus) + " != null");
			m_blackmagicCameraStatus.WriteClientCharacteristicConfigurationDescriptorAsync(
					GattClientCharacteristicConfigurationDescriptorValue.Notify).Completed =
				(a_result, _) =>
				{
					if (a_result.Status == AsyncStatus.Completed)
					{
						m_blackmagicCameraStatus.ValueChanged += OnCameraStatusValueChanged;
					}
					else
					{
						BlackmagicCameraLogInterface.LogError("Failed to subscribe to camera timecode service");
					}
				};

			Debug.Assert(m_blackmagicServiceTimecode != null, nameof(m_blackmagicServiceTimecode) + " != null");
			m_blackmagicServiceTimecode
					.WriteClientCharacteristicConfigurationDescriptorAsync(
						GattClientCharacteristicConfigurationDescriptorValue.Notify).Completed =
				(a_result, _) => {
					if (a_result.Status == AsyncStatus.Completed)
					{
						m_blackmagicServiceTimecode.ValueChanged += OnReceivedTimecode;
					}
					else
					{
						BlackmagicCameraLogInterface.LogError("Failed to subscribe to camera timecode service");
					}
				};
		}

		private void OnReceivedTimecode(GattCharacteristic a_sender, GattValueChangedEventArgs a_args)
		{
			using (Stream inputData = a_args.CharacteristicValue.AsStream())
			{
				CommandReader reader = new CommandReader(inputData);
				reader.ReadInt32();
				reader.ReadInt32();
				uint binaryTimecode = reader.ReadUInt32();
				LastReceivedTimeCode = TimeCode.FromBCD(binaryTimecode);
			}

			m_dispatcher.NotifyTimeCodeReceived(CameraDeviceHandle, LastReceivedTimeCode);
		}
		
		private void OnConnectionStatusChanged(BluetoothLEDevice a_sender, object a_args)
		{
			if (a_sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
			{
				if (!m_isInInitialReset)
				{
					ConnectionState = IBlackmagicCameraConnection.EConnectionState.Disconnected;
				}
			}
			else if (a_sender.ConnectionStatus == BluetoothConnectionStatus.Connected &&
			         m_isInInitialReset)
			{
				m_isInInitialReset = false;
			}
		}

		private void OnCameraStatusValueChanged(GattCharacteristic a_sender, GattValueChangedEventArgs a_args)
		{
			byte[] payload = a_args.CharacteristicValue.ToArray();
			EBluetoothCameraStatus cameraStatus = (EBluetoothCameraStatus) payload[0];
			if (cameraStatus == EBluetoothCameraStatus.None)
			{
				ConnectionState = IBlackmagicCameraConnection.EConnectionState.Disconnected;
			}

			BlackmagicCameraLogInterface.LogInfo("Received camera status: " + cameraStatus);
		}

		private void OnReceivedIncomingCameraControl(GattCharacteristic a_sender, GattValueChangedEventArgs a_args)
		{
			LastReceivedDataTime = a_args.Timestamp;

			//Deserialize and dispatch events.
			using Stream inputData = a_args.CharacteristicValue.AsStream();
			{
				using MemoryStream ms = new MemoryStream((int) inputData.Length);
				inputData.CopyTo(ms);

				m_dispatcher.NotifyRawDataReceived(CameraDeviceHandle, LastReceivedTimeCode, ms.ToArray());
			}

			ProcessCommandsFromStream(inputData, a_args.Timestamp);
		}

		private void OnReceivedOutgoingCameraControlResponse(GattCharacteristic a_sender, GattValueChangedEventArgs a_args)
		{
			LastReceivedDataTime = a_args.Timestamp;

			//Deserialize and dispatch events.
			using Stream inputData = a_args.CharacteristicValue.AsStream();
			ProcessCommandsFromStream(inputData, a_args.Timestamp);
		}

		private void ProcessCommandsFromStream(Stream a_inputData, DateTimeOffset a_receivedTime)
        {
            CommandReader.DecodeStream(a_inputData, (a_id, a_packet) => { m_dispatcher.NotifyDecodedDataReceived(CameraDeviceHandle, LastReceivedTimeCode, a_packet); });
        }

		public Task<string> AsyncRequestCameraModel()
		{
			if (m_deviceInformationCameraModel == null)
			{
				throw new Exception();
			}

			return Task.Run(async () =>
				{
					GattReadResult result = await m_deviceInformationCameraModel.ReadValueAsync(BluetoothCacheMode.Uncached);
					string resultString = Encoding.UTF8.GetString(result.Value.ToArray());
					return resultString;
				}
			);
		}

		public void AsyncSendCommand(ICommandPacketBase a_command, ECommandOperation a_commandOperation)
		{
			if (m_blackmagicServiceOutgoingCameraControl == null)
			{
				throw new Exception("Tried to send command that is not properly connected");
			}

			CommandMeta? commandMeta = CommandPacketFactory.FindCommandMeta(a_command.GetType());
			if (commandMeta == null)
			{
				throw new Exception("Tried to serialize command that is not known by factory");
			}

			long payloadSize = (CommandHeader.ByteSize + commandMeta.SerializedSizeBytes);
			long paddedPayloadSize = ((payloadSize + 3) & ~3);
			if (paddedPayloadSize > 0xFF)
			{
				throw new Exception("Command too big");
			}

			PacketHeader packetHeader = new PacketHeader
			{
				Command = EPacketCommand.ChangeConfig,
				PacketSize = (byte)paddedPayloadSize,
			};

			CommandHeader commandHeader = new CommandHeader
			{
				CommandIdentifier = commandMeta.Identifier,
				DataType = commandMeta.DataType,
				Operation = a_commandOperation
			};

			using (MemoryStream ms = new MemoryStream(64))
			{
				CommandWriter writer = new CommandWriter(ms);
				packetHeader.WriteTo(writer);
				commandHeader.WriteTo(writer);
				a_command.WriteTo(writer);

				IBuffer sendBuffer = WindowsRuntimeBuffer.Create(ms.GetBuffer(), 0, (int)ms.Length, (int)ms.Length);

				try
				{
					m_blackmagicServiceOutgoingCameraControl
						.WriteValueWithResultAsync(sendBuffer, GattWriteOption.WriteWithResponse).AsTask().ContinueWith(
							(a_sendCommand) =>
							{
								if (a_sendCommand.Result.Status != GattCommunicationStatus.Success)
								{
									BlackmagicCameraLogInterface.LogError(
										$"Failed to write value to outgoing camera control. Command: {commandMeta.CommandType}");
								}
							});
				}
				catch (COMException ex)
				{
					BlackmagicCameraLogInterface.LogError(
						$"Failed to write value to outgoing camera control. Command: {commandMeta.CommandType} Ex: {ex.Message}");

				}
			}
		}

		public BluetoothLEDevice GetDevice()
		{
			return m_device;
		}
	}
}
