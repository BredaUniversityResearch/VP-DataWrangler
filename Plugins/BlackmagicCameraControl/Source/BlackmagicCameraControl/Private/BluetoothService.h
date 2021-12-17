#pragma once

using BluetoothDeviceHandle = int;

class FBluetoothService
{
	class FBluetoothWorker;
	friend class FBluetoothWorkerRunnable;
public:
	FBluetoothService();
	~FBluetoothService();
	
	void QueryManufacturer(BluetoothDeviceHandle Target);
	void QueryCameraModel(BluetoothDeviceHandle Target);
	void QueryCameraSettings(BluetoothDeviceHandle Target);

private:
	TUniquePtr<FBluetoothWorker> Worker;
};

