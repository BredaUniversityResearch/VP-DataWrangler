#pragma once
#include "BMCCLens.h"

#include "BMCCCallbackHandler.generated.h"

struct FBMCCMedia_TransportMode;
struct FBMCCBattery_Info;

UINTERFACE(MinimalAPI, Blueprintable)
class UBMCCCallbackHandler : public UInterface
{
	GENERATED_BODY()
};

class IBMCCCallbackHandler
{
	GENERATED_BODY()
public:
	virtual void OnLensFocus(const FBMCCLens_Focus& Focus) {};
	virtual void OnLensApertureFStop(const FBMCCLens_ApertureFStop& Aperture) {};
	virtual void OnLensApertureNormalized(const FBMCCLens_ApertureNormalized& Aperture) {};
	virtual void OnLensApertureOrdinal(const FBMCCLens_ApertureOrdinal& Aperture) {};
	virtual void OnLensOpticalImageStabilization(const FBMCCLens_OpticalImageStabilization& ImageStabilization) {};
	virtual void OnLensAbsoluteZoomMm(const FBMCCLens_SetAbsoluteZoomMm& Zoom) {};
	virtual void OnLensAbsoluteZoomNormalized(const FBMCCLens_SetAbsoluteZoomNormalized& Zoom) {};
	virtual void OnLensContinuousZoom(const FBMCCLens_SetContinuousZoom& Zoom) {};

	virtual void OnBatteryStatus(const FBMCCBattery_Info& BatteryInfo) {};
	virtual void OnMediaTransportMode(const FBMCCMedia_TransportMode& TransportMode) {};
};
