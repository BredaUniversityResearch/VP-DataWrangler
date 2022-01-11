#pragma once
#include "BMCCDeviceHandle.h"
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
	virtual void OnLensFocus(BMCCDeviceHandle Source, const FBMCCLens_Focus& Focus) {};
	virtual void OnLensApertureFStop(BMCCDeviceHandle Source, const FBMCCLens_ApertureFStop& Aperture) {};
	virtual void OnLensApertureNormalized(BMCCDeviceHandle Source, const FBMCCLens_ApertureNormalized& Aperture) {};
	virtual void OnLensApertureOrdinal(BMCCDeviceHandle Source, const FBMCCLens_ApertureOrdinal& Aperture) {};
	virtual void OnLensOpticalImageStabilization(BMCCDeviceHandle Source, const FBMCCLens_OpticalImageStabilization& ImageStabilization) {};
	virtual void OnLensAbsoluteZoomMm(BMCCDeviceHandle Source, const FBMCCLens_SetAbsoluteZoomMm& Zoom) {};
	virtual void OnLensAbsoluteZoomNormalized(BMCCDeviceHandle Source, const FBMCCLens_SetAbsoluteZoomNormalized& Zoom) {};
	virtual void OnLensContinuousZoom(BMCCDeviceHandle Source, const FBMCCLens_SetContinuousZoom& Zoom) {};

	virtual void OnBatteryStatus(BMCCDeviceHandle Source, const FBMCCBattery_Info& BatteryInfo) {};
	virtual void OnMediaTransportMode(BMCCDeviceHandle Source, const FBMCCMedia_TransportMode& TransportMode) {};
};
