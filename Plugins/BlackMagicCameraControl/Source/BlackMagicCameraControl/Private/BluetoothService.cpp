#include "BluetoothService.h"

#include "WinRT.h"

namespace
{
	winrt::guid DeviceInformationService = winrt::Windows::Devices::Bluetooth::BluetoothUuidHelper::FromShortId(0x180A);
	winrt::guid DeviceInformationService_CameraManufacturer = winrt::Windows::Devices::Bluetooth::BluetoothUuidHelper::FromShortId(0x2A29);
	winrt::guid DeviceInformationService_CameraModel = winrt::Windows::Devices::Bluetooth::BluetoothUuidHelper::FromShortId(0x2A24);
};

using namespace winrt::Windows::Devices::Bluetooth;
using namespace winrt::Windows::Devices::Enumeration;

class FBluetoothService::FBluetoothWorker
{
public:
	FBluetoothWorker();

	void ShowAdvertisement(Advertisement::BluetoothLEAdvertisementReceivedEventArgs eventArgs);

	void OnDeviceAdded(DeviceWatcher watcher, const DeviceInformation& DeviceInfo);
	void OnDeviceRemoved(DeviceWatcher watcher, const DeviceInformationUpdate& DeviceInfo);

	Advertisement::BluetoothLEAdvertisementWatcher BLEAdvertisementWatcher;
	DeviceWatcher BLEDeviceWatcher;
};

FBluetoothService::FBluetoothWorker::FBluetoothWorker()
	: BLEDeviceWatcher(DeviceInformation::CreateWatcher(
		BluetoothLEDevice::GetDeviceSelectorFromPairingState(true),
		{}, DeviceInformationKind::Device))
{
	auto addedToken = BLEDeviceWatcher.Added([this](DeviceWatcher watcher, DeviceInformation info) { OnDeviceAdded(watcher, info); });
	auto removedToken = BLEDeviceWatcher.Removed([this](DeviceWatcher watcher, DeviceInformationUpdate info) { OnDeviceRemoved(watcher, info); });
	/*BLEDeviceWatcher.Added([this](DeviceWatcher watcher, DeviceInformation info)
	{
	});*/
	BLEDeviceWatcher.Start();

	auto recvToken = BLEAdvertisementWatcher.Received(
		[this](Advertisement::BluetoothLEAdvertisementWatcher watcher,
			Advertisement::BluetoothLEAdvertisementReceivedEventArgs eventArgs) {
				ShowAdvertisement(eventArgs);
		});
	BLEAdvertisementWatcher.Start();
}

void FBluetoothService::FBluetoothWorker::ShowAdvertisement(
	Advertisement::BluetoothLEAdvertisementReceivedEventArgs eventArgs)
{
	uint64_t addr = eventArgs.BluetoothAddress();
	Advertisement::BluetoothLEAdvertisementType advertisementType = eventArgs.AdvertisementType();
	int a = 6;

	Advertisement::BluetoothLEAdvertisement advertisement = eventArgs.Advertisement();
	auto serviceGuids = advertisement.ServiceUuids();
	uint32_t index;
	if (serviceGuids.IndexOf(DeviceInformationService, index))
	{
		//At least have device information service.6
	}

	/*
	 *std::wcout << L"Advertisement received from: " << AddrToString(eventArgs.BluetoothAddress());
	std::wcout << L" with signal strength " << eventArgs.RawSignalStrengthInDBm() << " dBm";
	std::wcout << L" Advertisement type: " << AdvertisementTypeToString(eventArgs.AdvertisementType()) << std::endl;

	BluetoothLEAdvertisement advertisement = eventArgs.Advertisement();
	auto dataSections = advertisement.DataSections();
	std::wcout << L" Number of data sections: " << dataSections.Size() << std::endl;
	for (BluetoothLEAdvertisementDataSection dataSection : dataSections) {
		std::wcout << L"  Data type: " << AdvertisementDataTypeToString(dataSection.DataType()) << std::endl;
	}*/
}

void FBluetoothService::FBluetoothWorker::OnDeviceAdded(DeviceWatcher watcher, const DeviceInformation& DeviceInfo)
{
	BluetoothLEDevice::FromIdAsync(DeviceInfo.Id()).Completed([](winrt::Windows::Foundation::IAsyncOperation<BluetoothLEDevice> connectedDevice, winrt::Windows::Foundation::AsyncStatus status) {
		int a = 7;
		connectedDevice.GetResults().GetGattServicesAsync().Completed(
			[](winrt::Windows::Foundation::IAsyncOperation<GenericAttributeProfile::GattDeviceServicesResult> servicesResult, winrt::Windows::Foundation::AsyncStatus status)
			{
				auto result = servicesResult.GetResults();
				if (result.Status() == GenericAttributeProfile::GattCommunicationStatus::Success)
				{
					auto services = result.Services();
					int b = 6;
				}
			});
		});
}

void FBluetoothService::FBluetoothWorker::OnDeviceRemoved(DeviceWatcher watcher, const DeviceInformationUpdate& DeviceInfo)
{
}

FBluetoothService::FBluetoothService()
	: Worker(MakeUnique<FBluetoothWorker>())
{
}

FBluetoothService::~FBluetoothService() = default;
