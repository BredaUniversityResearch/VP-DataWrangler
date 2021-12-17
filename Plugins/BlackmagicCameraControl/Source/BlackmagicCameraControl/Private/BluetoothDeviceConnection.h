#pragma once
#include "WinRT.h"
#include "BluetoothService.h"

using namespace winrt::Windows::Devices::Bluetooth;
using namespace GenericAttributeProfile;

class FBluetoothDeviceConnection
{
public:
	FBluetoothDeviceConnection(BluetoothDeviceHandle DeviceHandle, const BluetoothLEDevice& Device, 
	                           const GattDeviceService& DeviceInformationService, const GattDeviceService& BlackMagicService);

	bool IsValid() const;

	void QueryCameraManufacturer();
	void QueryCameraModel();
	void QueryCameraSettings();

	void OnQueryCameraManufacturerCompleted(const GattReadResult& result);
	void OnWriteResult(const GattCommunicationStatus& result);
	void OnReceivedIncomingCameraControl(const winrt::Windows::Storage::Streams::IBuffer& InputData);

	void SetupBlackMagicServiceCharacteristics();

	const BluetoothDeviceHandle m_DeviceHandle;
	BluetoothLEDevice m_Device;
	GattDeviceService m_DeviceInformationService;
	GattCharacteristic m_DeviceInformation_CameraManufacturer;
	GattCharacteristic m_DeviceInformation_CameraModel;
	GattDeviceService m_BlackMagicService;
	GattCharacteristic m_BlackMagicService_OutgoingCameraControl;
	GattCharacteristic m_BlackMagicService_IncomingCameraControl;
};
