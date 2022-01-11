#pragma once
#include "BMCCCallbackHandler.h"

#include "BMCCBattery_Info.h"
#include "BMCCMedia_TransportMode.h"

#include "BMCCDispatcher.generated.h"

DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnBatteryStatusReceived, const FBMCCBattery_Info&, Payload);
DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnMediaTransportModeReceived, const FBMCCMedia_TransportMode&, Payload);

UCLASS(BlueprintType)
class BLACKMAGICCAMERACONTROL_API UBMCCDispatcher
	: public UObject
	, public IBMCCCallbackHandler
{
	GENERATED_BODY()
public:
	virtual void OnBatteryStatus(BMCCDeviceHandle Source, const FBMCCBattery_Info& BatteryInfo) override;
	virtual void OnMediaTransportMode(BMCCDeviceHandle Source, const FBMCCMedia_TransportMode& TransportMode) override;

	UPROPERTY(BlueprintAssignable)
	FOnBatteryStatusReceived BatteryStatusReceived;
	UPROPERTY(BlueprintAssignable)
	FOnMediaTransportModeReceived MediaTransportModeReceived;
};
