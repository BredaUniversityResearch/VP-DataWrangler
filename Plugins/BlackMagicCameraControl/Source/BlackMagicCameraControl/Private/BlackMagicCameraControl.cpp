#include "BlackMagicCameraControl.h"

#define LOCTEXT_NAMESPACE "BlackMagicCameraControl"

DEFINE_LOG_CATEGORY(LogBlackMagicCameraControl);

void FBlackMagicCameraControl::StartupModule()
{
	ControlService = MakeUnique<FBlackMagicCameraControlService>();
	IModularFeatures::Get().RegisterModularFeature(FBlackMagicCameraControlService::GetModularFeatureName(), ControlService.Get());
}

void FBlackMagicCameraControl::ShutdownModule()
{
	IModularFeatures::Get().UnregisterModularFeature(FBlackMagicCameraControlService::GetModularFeatureName(), ControlService.Get());
	ControlService.Reset();
}

#undef LOCTEXT_NAMESPACE
	
IMPLEMENT_MODULE(FBlackMagicCameraControl, BlackMagicCameraControl)