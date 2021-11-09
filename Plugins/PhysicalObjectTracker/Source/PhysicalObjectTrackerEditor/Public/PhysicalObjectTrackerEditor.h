#pragma once

#include "CoreMinimal.h"

class FPhysicalObjectTrackingReferenceCalibrationHandler;

class FPhysicalObjectTrackerEditor : public IModuleInterface
{
public:

	/** IModuleInterface implementation */
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;
private:
	TUniquePtr<FPhysicalObjectTrackingReferenceCalibrationHandler> m_TrackingCalibrationHandler{};
};
