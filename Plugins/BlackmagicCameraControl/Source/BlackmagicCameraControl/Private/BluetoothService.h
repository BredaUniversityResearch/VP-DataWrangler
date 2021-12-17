#pragma once

#include "BMCCDeviceHandle.h"

class IBMCCDataReceivedHandler;
struct BMCCCommandHeader;

class FBluetoothService
{
	class FBluetoothWorker;
	friend class FBluetoothWorkerRunnable;
public:
	FBluetoothService(IBMCCDataReceivedHandler* DataReceivedHandler);
	~FBluetoothService();
	
	void QueryManufacturer(BMCCDeviceHandle Target);
	void QueryCameraModel(BMCCDeviceHandle Target);
	void QueryCameraSettings(BMCCDeviceHandle Target);

private:
	TUniquePtr<FBluetoothWorker> Worker;
};

