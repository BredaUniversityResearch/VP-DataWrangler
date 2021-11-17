#pragma once

#include "CoreMinimal.h"

DECLARE_LOG_CATEGORY_EXTERN(LogPhysicalObjectTracker, Log, All);
//DECLARE_DELEGATE_OneParam(FOnTrackerFound, uint32_t /*TrackerId*/)

DECLARE_EVENT_OneParam(FPhysicalObjectTracker, FDeviceDetectionStarted, class UPhysicalObjectTrackingComponent*)

class FDetectTrackerShakeTask;
class FPhysicalObjectTracker : public IModuleInterface
{
public:

	/** IModuleInterface implementation */
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;

	FDeviceDetectionStarted DeviceDetectionEvent;

	// Attempts to start the task to detect a tracker that is being shaked. Returns false if the attempt failed.
	//bool StartShakeDetectionTask(FOnTrackerFound OnTaskFinishedCallback);


};