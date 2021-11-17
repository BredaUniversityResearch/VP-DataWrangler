#pragma once

#include "CoreMinimal.h"

class FPhysicalObjectTrackingReferenceCalibrationHandler;
class FDetectTrackerShakeTask;

DECLARE_DELEGATE_OneParam(FShakeTaskFinished, uint32)

class FPhysicalObjectTrackerEditor : public IModuleInterface
{
public:

	/** IModuleInterface implementation */
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;


private:

	void StopDeviceSelection();

	TUniquePtr<FPhysicalObjectTrackingReferenceCalibrationHandler> m_TrackingCalibrationHandler{};

	TUniquePtr<FDetectTrackerShakeTask> m_ShakeDetectTask;
	TSharedPtr<SNotificationItem> m_ShakeProcessNotification;
};