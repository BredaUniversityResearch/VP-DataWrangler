#pragma once

#include "CoreMinimal.h"
#include "BlackmagicCameraControlService.h"

DECLARE_LOG_CATEGORY_EXTERN(LogBlackMagicCameraControl, Log, All);

class FBlackmagicCameraControl : public IModuleInterface
{
public:
	/** IModuleInterface implementation */
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;

private:
	TUniquePtr<FBlackmagicCameraControlService> ControlService;

};