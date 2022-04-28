using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using BlackmagicCameraControl.CommandPackets;

namespace BlackmagicCameraControl
{
	internal class BluetoothCameraConnection: IDisposable
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

		private readonly BlackmagicCameraController m_dispatcher;

		private readonly BluetoothLEDevice m_device;
		private readonly GattDeviceService m_deviceInformationService;
		private GattCharacteristic? m_deviceInformationCameraManufacturer;
		private GattCharacteristic? m_deviceInformationCameraModel;
		private readonly GattDeviceService m_blackmagicService;
		private GattCharacteristic? m_blackmagicServiceOutgoingCameraControl;
		private GattCharacteristic? m_blackmagicServiceIncomingCameraControl;

		private Task? m_connectingTask = null;

		public BluetoothCameraConnection(BlackmagicCameraController a_dispatcher, CameraHandle a_cameraHandle, BluetoothLEDevice a_target, GattDeviceService a_deviceInformationService,
			GattDeviceService a_blackmagicService)
		{
			CameraHandle = a_cameraHandle;
			m_dispatcher = a_dispatcher;
			m_device = a_target;
			m_deviceInformationService = a_deviceInformationService;
			m_blackmagicService = a_blackmagicService;

			m_connectingTask = Task.Run(async () => {
				GattCharacteristicsResult characteristics = await m_deviceInformationService.GetCharacteristicsAsync(BluetoothCacheMode.Cached);
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
					GattCharacteristicsResult blackmagicCharacteristics = await m_blackmagicService.GetCharacteristicsAsync(BluetoothCacheMode.Cached);

					if (characteristics.Status == GattCommunicationStatus.Success)
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
						}
					}
				}

				if (m_deviceInformationCameraManufacturer != null && m_deviceInformationCameraModel != null &&
				    m_blackmagicServiceOutgoingCameraControl != null &&
				    m_blackmagicServiceIncomingCameraControl != null)
				{
					SubscribeToIncomingServices();
					ConnectionState = EConnectionState.Connected;
				}
				else
				{
					ConnectionState = EConnectionState.Disconnected;
				}
			});
		}

		public void Dispose()
		{
			m_device.Dispose();
			m_deviceInformationService.Dispose();
			m_blackmagicService.Dispose();
			m_connectingTask?.Dispose();
		}

		public void WaitForConnection(TimeSpan a_timeout)
		{
			if (m_connectingTask != null)
			{
				if (!m_connectingTask.Wait(a_timeout))
				{
					throw new TimeoutException("Failed to connect to device in time");
				}
			}
		}

		private void SubscribeToIncomingServices()
		{
			GattClientCharacteristicConfigurationDescriptorValue newValue = GattClientCharacteristicConfigurationDescriptorValue.Indicate;
			
			Debug.Assert(m_blackmagicServiceIncomingCameraControl != null, nameof(m_blackmagicServiceIncomingCameraControl) + " != null");

			m_blackmagicServiceIncomingCameraControl.WriteClientCharacteristicConfigurationDescriptorAsync(newValue).Completed = (_, _) =>
			{
				m_blackmagicServiceIncomingCameraControl.ValueChanged += OnReceivedIncomingCameraControl;
			};
		}

		private void OnReceivedIncomingCameraControl(GattCharacteristic a_sender, GattValueChangedEventArgs a_args)
		{
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

						int commandSize = CommandPacketFactory.GetSerializedCommandSize(header.CommandIdentifier);
						if (reader.BytesRemaining < commandSize)
						{
							throw new Exception();
						}

						ICommandPacketBase? packetInstance = CommandPacketFactory.CreatePacket(header.CommandIdentifier, reader);
						if (packetInstance != null)
						{
							m_dispatcher.NotifyDataReceived(CameraHandle, packetInstance);
						}
						else
						{
							BlackmagicCameraLogInterface.LogWarning($"Received unknown packet with identifier {header.CommandIdentifier}");
						}
					}
				}
			}
		}
	}
}
