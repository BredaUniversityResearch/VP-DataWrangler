#include "BMCCService.h"

#include "BlackmagicCameraControl.h"
#include "BluetoothService.h"
#include "BMCCCallbackHandler.h"
#include "BMCCDispatcher.h"
#include "BMCCBattery_Info.h"
#include "BMCCCommandHeader.h"
#include "BMCCCommandMeta.h"

class FBMCCService::Pimpl
{
public:
	TUniquePtr<FBluetoothService> m_BluetoothService;

	TArray<IBMCCCallbackHandler*> CallbackHandlers;
};

FBMCCService::FBMCCService()
	: m_Data(MakeUnique<Pimpl>())
	, DefaultDispatcher(NewObject<UBMCCDispatcher>())
{
	DefaultDispatcher->AddToRoot();
	m_Data->m_BluetoothService = MakeUnique<FBluetoothService>(this);
}

FBMCCService::~FBMCCService()
{
	DefaultDispatcher->RemoveFromRoot();
}

void FBMCCService::Tick(float DeltaTime)
{
	static float dtAccumulator = 0;
	dtAccumulator += DeltaTime;
	if (dtAccumulator > 1.0f)
	{
		BroadcastCommand(FBMCCLens_Focus{});
		BroadcastCommand(FBMCCLens_TriggerAutoFocus{});
		BroadcastCommand(FBMCCLens_ApertureFStop{});
		BroadcastCommand(FBMCCLens_ApertureNormalized{});
		BroadcastCommand(FBMCCLens_ApertureOrdinal{});
		BroadcastCommand(FBMCCLens_TriggerInstantAutoAperture{});
		BroadcastCommand(FBMCCLens_OpticalImageStabilization{});
		BroadcastCommand(FBMCCLens_SetAbsoluteZoomMm{});
		BroadcastCommand(FBMCCLens_SetAbsoluteZoomNormalized{});
		BroadcastCommand(FBMCCLens_SetContinuousZoom{});
		BroadcastCommand(FBMCCBattery_Info{});
		dtAccumulator = 0.0f;
	}
}

void FBMCCService::OnDataReceived(BMCCDeviceHandle Source, const BMCCCommandHeader& Header, const FBMCCCommandMeta& CommandMetaData, const TArrayView<uint8_t>& SerializedData)
{
	CommandMetaData.DeserializeAndDispatch(m_Data->CallbackHandlers, SerializedData);
}

void FBMCCService::BroadcastCommand(const FBMCCCommandIdentifier& Identifier,
	const FBMCCCommandPayloadBase& Command) const
{
	m_Data->m_BluetoothService->SendToCamera(BMCCDeviceHandle_Broadcast, Identifier, Command);
}

UBMCCDispatcher* FBMCCService::GetDefaultDispatcher() const
{
	return DefaultDispatcher;
}
