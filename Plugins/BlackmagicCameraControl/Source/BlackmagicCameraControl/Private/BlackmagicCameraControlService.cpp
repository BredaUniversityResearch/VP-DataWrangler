#include "BlackmagicCameraControlService.h"

#include "BluetoothService.h"
#include "BlackmagicCameraControlCallbackHandler.h"
#include "BMCCAllPackets.h"
#include "BMCCCommandHeader.h"

class FBlackmagicCameraControlService::Pimpl
{
public:
	TUniquePtr<FBluetoothService> m_BluetoothService;

	TArray<IBlackmagicCameraControlCallbackHandler*> CallbackHandlers;
};

FBlackmagicCameraControlService::FBlackmagicCameraControlService()
	: m_Data(MakeUnique<Pimpl>())
{
	m_Data->m_BluetoothService = MakeUnique<FBluetoothService>(this);
}

FBlackmagicCameraControlService::~FBlackmagicCameraControlService() = default; //So we can use a unique ptr

void FBlackmagicCameraControlService::OnDataReceived(BMCCDeviceHandle Source, const BMCCCommandHeader& Header, const TArrayView<uint8_t>& SerializedData)
{
	const FBMCCCommandMeta* packetMeta = FBMCCAllPackets::FindMetaForIdentifier(Header.Identifier);
	if (packetMeta != nullptr)
	{
		//packetMeta->DeserializeAndDispatch(SerializedData);
	}
}
