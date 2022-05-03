using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Storage.Streams;
using BlackmagicCameraControl.CommandPackets;
using WinRT;
using Buffer = Windows.Storage.Streams.Buffer;

namespace BlackmagicCameraControl
{
	internal class BluetoothCameraConnection : IDisposable
	{
		public enum EConnectionState
		{
			QueryingProperties,
			Connected,
			Disconnected,
		};

		public EConnectionState ConnectionState { get; private set; }
		public string DeviceId => m_device.DeviceId;

		public readonly CameraHandle CameraHandle;
		public DateTimeOffset LastReceivedDataTime { get; private set; }

		private readonly BlackmagicCameraController m_dispatcher;

		private readonly BluetoothLEDevice m_device;
		private readonly GattDeviceService m_deviceInformationService;
		private GattCharacteristic? m_deviceInformationCameraManufacturer;
		private GattCharacteristic? m_deviceInformationCameraModel;
		private readonly GattDeviceService m_blackmagicService;
		private GattCharacteristic? m_blackmagicServiceOutgoingCameraControl;
		private GattCharacteristic? m_blackmagicServiceIncomingCameraControl;
		private GattCharacteristic? m_blackmagicServiceTimecode;

		private Task? m_connectingTask = null;

		public BluetoothCameraConnection(BlackmagicCameraController a_dispatcher, CameraHandle a_cameraHandle, BluetoothLEDevice a_target, GattDeviceService a_deviceInformationService,
			GattDeviceService a_blackmagicService)
		{
			CameraHandle = a_cameraHandle;
			m_dispatcher = a_dispatcher;
			m_device = a_target;
			m_deviceInformationService = a_deviceInformationService;
			m_blackmagicService = a_blackmagicService;

			m_device.ConnectionStatusChanged += OnConnectionStatusChanged;

			m_connectingTask = Task.Run(async () =>
			{
				GattCharacteristicsResult characteristics = await m_deviceInformationService.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
				if (characteristics.Status == GattCommunicationStatus.Success)
				{
					foreach (GattCharacteristic characteristic in characteristics.Characteristics)
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
					GattCharacteristicsResult blackmagicCharacteristics = await m_blackmagicService.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);

					if (blackmagicCharacteristics.Status == GattCommunicationStatus.Success)
					{
						foreach (GattCharacteristic characteristic in blackmagicCharacteristics.Characteristics)
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
						}
					}
				}

				if (m_deviceInformationCameraManufacturer != null && 
				    m_deviceInformationCameraModel != null &&
					m_blackmagicServiceOutgoingCameraControl != null &&
					m_blackmagicServiceIncomingCameraControl != null &&
					m_blackmagicServiceTimecode != null)
				{
					SubscribeToIncomingServices();
					LastReceivedDataTime = DateTimeOffset.UtcNow;
					ConnectionState = EConnectionState.Connected;
				}
				else
				{
					ConnectionState = EConnectionState.Disconnected;
				}
			});	
		}

		private void OnConnectionStatusChanged(BluetoothLEDevice a_sender, object a_args)
		{
			if (a_sender.ConnectionStatus == BluetoothConnectionStatus.Disconnected)
			{
				ConnectionState = EConnectionState.Disconnected;
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

			return ConnectionState == EConnectionState.Connected;
		}

		private void SubscribeToIncomingServices()
		{
			GattClientCharacteristicConfigurationDescriptorValue newValue = GattClientCharacteristicConfigurationDescriptorValue.Indicate;

			Debug.Assert(m_blackmagicServiceIncomingCameraControl != null, nameof(m_blackmagicServiceIncomingCameraControl) + " != null");

			m_blackmagicServiceIncomingCameraControl.WriteClientCharacteristicConfigurationDescriptorAsync(newValue).Completed = (_, _) =>
			{
				m_blackmagicServiceIncomingCameraControl.ValueChanged += OnReceivedIncomingCameraControl;
			};

			//m_blackmagicServiceTimecode
			//		.WriteClientCharacteristicConfigurationDescriptorAsync(
			//			GattClientCharacteristicConfigurationDescriptorValue.Notify).Completed =
			//	(a_result, _) => {
			//		if (a_result.Status == AsyncStatus.Completed)
			//		{
			//			m_blackmagicServiceTimecode.ValueChanged += OnReceivedTimecode;
			//		}
			//		else
			//		{
			//			IBlackmagicCameraLogInterface.LogError("Failed to subscribe to camera timecode service");
			//		}
			//	};
		}

		private void OnReceivedTimecode(GattCharacteristic a_sender, GattValueChangedEventArgs a_args)
		{
			using (Stream inputData = a_args.CharacteristicValue.AsStream())
			{
				CommandReader reader = new CommandReader(inputData);
				reader.ReadInt32();
				reader.ReadInt32();
				int binaryTimecode = reader.ReadInt32();

			}
		}

		private void OnReceivedIncomingCameraControl(GattCharacteristic a_sender, GattValueChangedEventArgs a_args)
		{
			LastReceivedDataTime = a_args.Timestamp;

			//Deserialize and dispatch events.
			using (Stream inputData = a_args.CharacteristicValue.AsStream())
			{
				CommandReader reader = new CommandReader(inputData);
				while (reader.BytesRemaining >= PacketHeader.ByteSize)
				{
					PacketHeader packetHeader = reader.ReadPacketHeader();
					if (reader.BytesRemaining > packetHeader.PacketSize && reader.BytesRemaining < byte.MaxValue)
					{
						throw new Exception();
					}

					while (reader.BytesRemaining > CommandHeader.ByteSize)
					{
						CommandHeader header = reader.ReadCommandHeader();

						CommandMeta? commandMeta = CommandPacketFactory.FindCommandMeta(header.CommandIdentifier);
						if (commandMeta == null)
						{
							IBlackmagicCameraLogInterface.LogWarning($"Received unknown packet with identifier {header.CommandIdentifier}. Size: {reader.BytesRemaining}, Type: {header.DataType}");
							inputData.Seek(packetHeader.PacketSize - CommandHeader.ByteSize, SeekOrigin.Current);
							break;
						}

						int possiblePadding = packetHeader.PacketSize - ((int)CommandHeader.ByteSize + commandMeta.SerializedSizeBytes);

						if (header.DataType != commandMeta.DataType || ((reader.BytesRemaining - commandMeta.SerializedSizeBytes) > possiblePadding && header.DataType != ECommandDataType.Utf8String))
						{
							throw new Exception($"Command meta data wrong: Bytes (Expected / Got) {commandMeta.SerializedSizeBytes} / {reader.BytesRemaining}, DataType: {commandMeta.DataType} / {header.DataType}");
						}

						ICommandPacketBase? packetInstance = CommandPacketFactory.CreatePacket(header.CommandIdentifier, reader);
						if (packetInstance != null)
						{
							IBlackmagicCameraLogInterface.LogVerbose($"Received Packet {header.CommandIdentifier}. {packetInstance}");
							m_dispatcher.NotifyDataReceived(CameraHandle, a_args.Timestamp, packetInstance);
						}
						else
						{
							throw new Exception("Failed to deserialize command with known meta");
						}
					}
				}
			}
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

		public void AsyncSendCommand(ICommandPacketBase a_command)
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
				Operation = ECommandOperation.Assign
			};

			using (MemoryStream ms = new MemoryStream(64))
			{
				CommandWriter writer = new CommandWriter(ms);
				packetHeader.WriteTo(writer);
				commandHeader.WriteTo(writer);
				a_command.WriteTo(writer);

				IBuffer sendBuffer = WindowsRuntimeBuffer.Create(ms.GetBuffer(), 0, (int)ms.Length, (int)ms.Length);
				m_blackmagicServiceOutgoingCameraControl
					.WriteValueAsync(sendBuffer, GattWriteOption.WriteWithResponse).AsTask().ContinueWith(
						(a_sendCommand) =>
						{
							if (a_sendCommand.Result != GattCommunicationStatus.Success)
							{
								IBlackmagicCameraLogInterface.LogError(
									$"Failed to write value to outgoing camera control. Command: {commandMeta.CommandType}");
							}
						});
			}
		}

		public BluetoothLEDevice GetDevice()
		{
			return m_device;
		}
	}
}
