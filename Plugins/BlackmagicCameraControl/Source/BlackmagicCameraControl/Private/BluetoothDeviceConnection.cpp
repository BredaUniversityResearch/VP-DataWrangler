#include "BluetoothDeviceConnection.h"

#include <iostream>
#include <ostream>
#include <span>

#include "AsyncWrapper.h"
#include "BlackMagicBluetoothGUID.h"

using namespace winrt::Windows::Storage::Streams;
using namespace winrt::Windows::Foundation;

namespace
{
	enum class ECommandId : uint8_t
	{
		ChangeConfig,
	};

	enum class EDataType : uint8_t
	{
		VoidOrBool = 0,
		Int8 = 1,
		Int16 = 2,
		Int32 = 3,
		Int64 = 4,
		Utf8String = 5,
		Signed5_11FixedPoint = 128 //int16 5:11 Fixed point
	};

	enum class EOperation : uint8_t
	{
		Assign = 0,
		OffsetOrToggle = 1
	};

	struct PacketHeader
	{
		uint8_t TargetCamera{ 255 };//255 is broadcast
		uint8_t PacketSize{ 0 };
		ECommandId CommandId{ ECommandId::ChangeConfig };
		uint8_t Reserved{ 0 };
	};
	static_assert(sizeof(PacketHeader) == 4, "Packet header is expected to be 4 bytes");

	struct CommandHeader
	{
		uint8_t Category{ 10 };
		uint8_t Parameter{ 1 };
		EDataType DataType{ EDataType::Int8 };
		EOperation Operation{ EOperation::Assign };
	};
	static_assert(sizeof(CommandHeader) == 4, "Command header is expected to be 4 bytes");

	struct Media_TransportMode
	{
		enum class EMode : uint8_t
		{
			Preview = 0,
			Play = 1,
			Record = 2
		};
		enum class EFlags: uint8_t
		{
			Loop = (1 << 0),
			PlayAll = (1 << 1),
			Disk1Active = (1 << 5),
			Disk2Active = (1 << 6),
			TimeLapseRecording = (1 << 7)
		};
		enum class EStorageTarget : uint8_t
		{
			CFast,
			SD
		};

		EMode Mode{ EMode::Preview }; //Preview Play Record
		uint8_t PlaybackSpeed{ 1 };
		EFlags Flags{ 0 };	
		EStorageTarget TargetStorageMedium{ EStorageTarget::CFast }; 
	};
	static_assert(sizeof(Media_TransportMode) == 4, "Transport mode command is expected to be 4 bytes");

	template<typename TPayload>
	void CreateCommandPackage(const TPayload& CommandPayload, Buffer& TargetBuffer)
	{
		PacketHeader header;
		int payloadSize = static_cast<int>((sizeof(PacketHeader) + sizeof(CommandHeader) + sizeof(CommandPayload)));
		int padBytes = ((payloadSize + 3) & ~3) - payloadSize;
		assert(payloadSize + padBytes < 0xFF);
		header.PacketSize = static_cast<uint8_t>(payloadSize - sizeof(PacketHeader));

		CommandHeader commandHeader;

		uint8_t* data = TargetBuffer.data();
		std::memcpy(data, &header, sizeof(PacketHeader));
		std::memcpy(data + sizeof(PacketHeader), &commandHeader, sizeof(commandHeader));
		std::memcpy(data + sizeof(PacketHeader) + sizeof(CommandHeader), &CommandPayload, sizeof(CommandPayload));

		TargetBuffer.Length(payloadSize + padBytes);
	}

	/*Buffer CreateCommandPackage(std::span<uint8_t> Payload)
	{
		PacketHeader header;
		int payloadSize = static_cast<int>((sizeof(CommandHeader) + Payload.size_bytes()));
		int padBytes = ((payloadSize + 3) & ~3) - payloadSize;
		assert(payloadSize + padBytes < 0xFF);
		header.PacketSize = static_cast<uint8_t>(payloadSize);

		Buffer result(payloadSize + padBytes);
		uint8_t* data = result.data();
		std::memcpy(data, &header, sizeof(PacketHeader));
		std::memcpy(data + sizeof(CommandHeader), Payload.data(), Payload.size_bytes());

		result.Length(payloadSize + padBytes);

		return result;
	}*/
};

FBluetoothDeviceConnection::FBluetoothDeviceConnection(BluetoothDeviceHandle DeviceHandle, const BluetoothLEDevice& Device, const GattDeviceService& DeviceInformationService, const GattDeviceService& BlackMagicService)
	: m_DeviceHandle(DeviceHandle)
	, m_Device(Device)
	, m_DeviceInformationService(DeviceInformationService)
	, m_DeviceInformation_CameraManufacturer(DeviceInformationService.GetCharacteristics(BMBTGUID::DeviceInformationService_CameraManufacturer).GetAt(0))
	, m_DeviceInformation_CameraModel(DeviceInformationService.GetCharacteristics(BMBTGUID::DeviceInformationService_CameraModel).GetAt(0))
	, m_BlackMagicService(BlackMagicService)
	, m_BlackMagicService_OutgoingCameraControl(nullptr)
	, m_BlackMagicService_IncomingCameraControl(nullptr)
{
	SetupBlackMagicServiceCharacteristics();
}

bool FBluetoothDeviceConnection::IsValid() const
{
	return m_BlackMagicService_IncomingCameraControl != nullptr && m_BlackMagicService_OutgoingCameraControl != nullptr;
}

void FBluetoothDeviceConnection::QueryCameraManufacturer()
{
	BluetoothConnectionStatus status = m_Device.ConnectionStatus();
	IAsyncOperation<GattReadResult> result = m_DeviceInformation_CameraManufacturer.ReadValueAsync(
		BluetoothCacheMode::Uncached);
	result.Completed(AsyncWrapper(this, &FBluetoothDeviceConnection::OnQueryCameraManufacturerCompleted));
}

void FBluetoothDeviceConnection::QueryCameraModel()
{
	IAsyncOperation<GattReadResult> result = m_DeviceInformation_CameraModel.ReadValueAsync(
		BluetoothCacheMode::Uncached);
	result.Completed(AsyncWrapper(this, &FBluetoothDeviceConnection::OnQueryCameraManufacturerCompleted));
}

void FBluetoothDeviceConnection::QueryCameraSettings()
{
	Media_TransportMode payload;
	Buffer buffer(128);
	CreateCommandPackage(payload, buffer);

	IAsyncOperation<GattCommunicationStatus> result = m_BlackMagicService_OutgoingCameraControl.WriteValueAsync(buffer, GattWriteOption::WriteWithResponse);
	result.Completed(AsyncWrapper(this, &FBluetoothDeviceConnection::OnWriteResult));
}

void FBluetoothDeviceConnection::OnQueryCameraManufacturerCompleted(const GattReadResult& result)
{
	std::cout << reinterpret_cast<const char*>(result.Value().data()) << std::endl;
}
	
void FBluetoothDeviceConnection::OnWriteResult(const GattCommunicationStatus& result)
{
	std::wcout << "Write Result " << winrt::to_hstring(static_cast<int>(result)).c_str()<< std::endl;
}

void FBluetoothDeviceConnection::OnReceivedIncomingCameraControl(const IBuffer& InputData)
{
	int bytesProcessed = 0;
	const PacketHeader* packet = reinterpret_cast<const PacketHeader*>(InputData.data());
	bytesProcessed += sizeof(PacketHeader);
	while (bytesProcessed + sizeof(CommandHeader) < packet->PacketSize)
	{
		const CommandHeader* command = reinterpret_cast<const CommandHeader*>(InputData.data() + bytesProcessed);
		bytesProcessed += sizeof(CommandHeader);

		const int16_t* payloadData = reinterpret_cast<const int16_t*>(InputData.data() + bytesProcessed);
		//9.0
		//int16 battery mV
		//int16 battery percentage
		//int16 unknown, hours battery life remaining? 
 		std::wcout << "ID: " << winrt::to_hstring(command->Category).c_str() << L"." << winrt::to_hstring(command->Parameter).c_str() << std::endl;
	}
}


void FBluetoothDeviceConnection::SetupBlackMagicServiceCharacteristics()
{
	m_BlackMagicService.GetCharacteristicsAsync().Completed([this](const IAsyncOperation<GattCharacteristicsResult>& ResultOp, AsyncStatus) {
		GattCharacteristicsResult result = ResultOp.GetResults();
		if (result.Status() == GattCommunicationStatus::Success)
		{
			for (const GattCharacteristic& characteristic : result.Characteristics())
			{
				if (characteristic.Uuid() == BMBTGUID::BlackMagicService_OutgoingCameraControl)
				{
					m_BlackMagicService_OutgoingCameraControl = characteristic;
				}
				else if (characteristic.Uuid() == BMBTGUID::BlackMagicService_IncomingCameraControl)
				{
					m_BlackMagicService_IncomingCameraControl = characteristic;
				}
			}
			if (m_BlackMagicService_IncomingCameraControl != nullptr && m_BlackMagicService_OutgoingCameraControl != nullptr)
			{
				GattClientCharacteristicConfigurationDescriptorValue newValue = GattClientCharacteristicConfigurationDescriptorValue::Indicate;
				m_BlackMagicService_IncomingCameraControl.WriteClientCharacteristicConfigurationDescriptorAsync(newValue).Completed([this](IAsyncOperation<GattCommunicationStatus>, AsyncStatus)
					{
						m_BlackMagicService_IncomingCameraControl.ValueChanged([this](const GattCharacteristic&, const GattValueChangedEventArgs& Args) {
							OnReceivedIncomingCameraControl(Args.CharacteristicValue());
							});
					});
			}
			else
			{
				std::wcout << L"One or mor BlackMagic service characteristics was not found" << std::endl;
			}
		}
		else
		{
			std::wcout << L"One or mor BlackMagic service characteristics was not found. Communication did not return SUCCESS" << std::endl;
		}
	});
}
