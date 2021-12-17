#include "BlackmagicCameraControl.h"

#define LOCTEXT_NAMESPACE "BlackMagicCameraControl"

DEFINE_LOG_CATEGORY(LogBlackMagicCameraControl);

void FBlackmagicCameraControl::StartupModule()
{
	ControlService = MakeUnique<FBlackmagicCameraControlService>();
	IModularFeatures::Get().RegisterModularFeature(FBlackmagicCameraControlService::GetModularFeatureName(), ControlService.Get());
}

void FBlackmagicCameraControl::ShutdownModule()
{
	IModularFeatures::Get().UnregisterModularFeature(FBlackmagicCameraControlService::GetModularFeatureName(), ControlService.Get());
	ControlService.Reset();
}

#undef LOCTEXT_NAMESPACE
	
IMPLEMENT_MODULE(FBlackmagicCameraControl, BlackMagicCameraControl)