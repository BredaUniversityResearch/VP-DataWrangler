#include "BlackmagicCameraControlService.h"

#include "BluetoothService.h"

class FBlackmagicCameraControlService::Pimpl
{
public:
	TUniquePtr<FBluetoothService> m_BluetoothService;
};

FBlackmagicCameraControlService::FBlackmagicCameraControlService()
	: m_Data(MakeUnique<Pimpl>())
{
	m_Data->m_BluetoothService = MakeUnique<FBluetoothService>();
}

FBlackmagicCameraControlService::~FBlackmagicCameraControlService() = default; //So we can use a unique ptr
