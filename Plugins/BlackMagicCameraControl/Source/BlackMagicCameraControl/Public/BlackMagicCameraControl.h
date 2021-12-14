#pragma once

#include "CoreMinimal.h"
#include "BlackMagicCameraControlService.h"

DECLARE_LOG_CATEGORY_EXTERN(LogBlackMagicCameraControl, Log, All);

class FBlackMagicCameraControl : public IModuleInterface
{
public:
	/** IModuleInterface implementation */
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;

private:
	TUniquePtr<FBlackMagicCameraControlService> ControlService;

};