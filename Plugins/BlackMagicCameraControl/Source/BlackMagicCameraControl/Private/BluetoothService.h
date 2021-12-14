#pragma once

class FBluetoothService
{
	class FBluetoothWorker;
public:
	FBluetoothService();
	~FBluetoothService();

private:
	TUniquePtr<FBluetoothWorker> Worker;
};