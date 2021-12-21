#include "BluetoothDeviceConnection.h"

#include "AsyncWrapper.h"
#include "BlackMagicBluetoothGUID.h"
#include "BMCCCommandHeader.h"
#include "BMCCCommandMeta.h"

using namespace winrt::Windows::Storage::Streams;
using namespace winrt::Windows::Foundation;

namespace
{
	enum class ECommandId : uint8_t
	{
		ChangeConfig,
	};

	struct PacketHeader
	{
		uint8_t TargetCamera{ 255 };//255 is broadcast
		uint8_t PacketSize{ 0 };
		ECommandId CommandId{ ECommandId::ChangeConfig };
		uint8_t Reserved{ 0 };
	};
	static_assert(sizeof(PacketHeader) == 4, "Packet header is expected to be 4 bytes");

	void CreateCommandPackage(const FBMCCCommandMeta& Meta, const FBMCCCommandPayloadBase& Payload, Buffer& TargetBuffer)
	{
		PacketHeader header;
		int payloadSize = static_cast<int>((sizeof(PacketHeader) + sizeof(BMCCCommandHeader) + Meta.PayloadSize));
		int padBytes = ((payloadSize + 3) & ~3) - payloadSize;
		assert(payloadSize + padBytes < 0xFF);
		header.PacketSize = static_cast<uint8_t>(payloadSize - sizeof(PacketHeader));

		BMCCCommandHeader commandHeader(Meta.CommandIdentifier);

		uint8_t* data = TargetBuffer.data();
		std::memcpy(data, &header, sizeof(PacketHeader));
		std::memcpy(data + sizeof(PacketHeader), &commandHeader, sizeof(commandHeader));
		std::memcpy(data + sizeof(PacketHeader) + sizeof(BMCCCommandHeader), &Payload, Meta.PayloadSize);

		TargetBuffer.Length(payloadSize + padBytes);
	}

	/*Buffer CreateCommandPackage(std::span<uint8_t> Payload)
	{
		PacketHeader header;
		int payloadSize = static_cast<int>((sizeof(BMCCCommandHeader) + Payload.size_bytes()));
		int padBytes = ((payloadSize + 3) & ~3) - payloadSize;
		assert(payloadSize + padBytes < 0xFF);
		header.PacketSize = static_cast<uint8_t>(payloadSize);

		Buffer result(payloadSize + padBytes);
		uint8_t* data = result.data();
		std::memcpy(data, &header, sizeof(PacketHeader));
		std::memcpy(data + sizeof(BMCCCommandHeader), Payload.data(), Payload.size_bytes());

		result.Length(payloadSize + padBytes);

		return result;
	}*/
};

FBluetoothDeviceConnection::FBluetoothDeviceConnection(BMCCDeviceHandle DeviceHandle, IBMCCDataReceivedHandler* DataReceivedHandler, const BluetoothLEDevice& Device, const GattDeviceService& DeviceInformationService, const GattDeviceService& BlackMagicService)
	: m_DeviceHandle(DeviceHandle)
	, m_Device(Device)
	, m_DeviceInformationService(DeviceInformationService)
	, m_DeviceInformation_CameraManufacturer(DeviceInformationService.GetCharacteristics(BMBTGUID::DeviceInformationService_CameraManufacturer).GetAt(0))
	, m_DeviceInformation_CameraModel(DeviceInformationService.GetCharacteristics(BMBTGUID::DeviceInformationService_CameraModel).GetAt(0))
	, m_BlackMagicService(BlackMagicService)
	, m_BlackMagicService_OutgoingCameraControl(nullptr)
	, m_BlackMagicService_IncomingCameraControl(nullptr)
	, m_DataReceivedHandler(DataReceivedHandler)
{
	SetupBlackMagicServiceCharacteristics();
}

FBluetoothDeviceConnection::FBluetoothDeviceConnection(BMCCDeviceHandle DeviceHandle, IBMCCDataReceivedHandler* CallbackService, ELoopbackDevice)
	: m_DeviceHandle(DeviceHandle)
	, m_Device(nullptr)
	, m_DeviceInformationService(nullptr)
	, m_DeviceInformation_CameraManufacturer(nullptr)
	, m_DeviceInformation_CameraModel(nullptr)
	, m_BlackMagicService(nullptr)
	, m_BlackMagicService_OutgoingCameraControl(nullptr)
	, m_BlackMagicService_IncomingCameraControl(nullptr)
	, m_DataReceivedHandler(CallbackService)
{
	UE_LOG(LogBlackmagicCameraControl, Warning, TEXT("Creating Blackmagic Camera Control loopback device with handle %i"), m_DeviceHandle);
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

void FBluetoothDeviceConnection::SendCommand(const FBMCCCommandMeta& Meta, const FBMCCCommandPayloadBase& Payload) const
{
	Buffer buffer(128);
	CreateCommandPackage(Meta, Payload, buffer);

	if (m_Device != nullptr)
	{
		IAsyncOperation<GattCommunicationStatus> result = m_BlackMagicService_OutgoingCameraControl.WriteValueAsync(buffer, GattWriteOption::WriteWithResponse);
		result.Completed(AsyncWrapper(this, &FBluetoothDeviceConnection::OnWriteResult));
	}
	else
	{
		OnReceivedIncomingCameraControl(buffer);
	}
}

void FBluetoothDeviceConnection::OnQueryCameraManufacturerCompleted(const GattReadResult& result)
{
	UE_LOG(LogBlackmagicCameraControl, Warning, TEXT("%s"), ANSI_TO_TCHAR(reinterpret_cast<const char*>(result.Value().data())));
}

void FBluetoothDeviceConnection::OnWriteResult(const GattCommunicationStatus& result) const
{
	UE_LOG(LogBlackmagicCameraControl, Warning, TEXT("Write Result %s"), winrt::to_hstring(static_cast<int>(result)).c_str());
}

void FBluetoothDeviceConnection::OnReceivedIncomingCameraControl(const IBuffer& InputData) const
{
	int bytesProcessed = 0;
	const PacketHeader* packet = reinterpret_cast<const PacketHeader*>(InputData.data());
	bytesProcessed += sizeof(PacketHeader);
	while (InputData.Length() - bytesProcessed >= packet->PacketSize)
	{
		const BMCCCommandHeader* command = reinterpret_cast<const BMCCCommandHeader*>(InputData.data() + bytesProcessed);
		bytesProcessed += sizeof(BMCCCommandHeader);
		const FBMCCCommandMeta* meta = FBMCCCommandMeta::FindMetaForIdentifier(command->Identifier);
		if (meta != nullptr)
		{
			if (m_DataReceivedHandler != nullptr)
			{
				ensureMsgf(bytesProcessed - sizeof(PacketHeader) + meta->PayloadSize <= packet->PacketSize, TEXT("Metadata mentions payload that is bigger than the actual packet size..."));
				m_DataReceivedHandler->OnDataReceived(m_DeviceHandle, *command, *meta, TArrayView<uint8_t>(InputData.data() + bytesProcessed, meta->PayloadSize));
			}
			bytesProcessed += meta->PayloadSize;
		}
		else
		{
			UE_LOG(LogBlackmagicCameraControl, Error, TEXT("Failed to find packet meta for ID %i.%i"), command->Identifier.Category, command->Identifier.Parameter);
			break;
		}
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
				UE_LOG(LogBlackmagicCameraControl, Error, TEXT("One or mor BlackMagic service characteristics was not found"));
			}
		}
		else
		{
			UE_LOG(LogBlackmagicCameraControl, Error, TEXT("One or more BlackMagic service characteristics was not found. Communication did not return SUCCESS"));
		}
		});
}
