#include "BlackMagicCameraControlService.h"

#include "BluetoothService.h"

class FBlackMagicCameraControlService::Pimpl
{
public:
	TUniquePtr<FBluetoothService> m_BluetoothService;
};

FBlackMagicCameraControlService::FBlackMagicCameraControlService()
	: m_Data(MakeUnique<Pimpl>())
{
	m_Data->m_BluetoothService = MakeUnique<FBluetoothService>();
}

FBlackMagicCameraControlService::~FBlackMagicCameraControlService() = default; //So we can use a unique ptr
