#pragma once
#include "BMCCCallbackHandler.h"

#include "BMCCBattery_Info.h"

#include "BMCCDispatcher.generated.h"

DECLARE_DYNAMIC_MULTICAST_DELEGATE_OneParam(FOnBatteryStatusReceived, const FBMCCBattery_Info&, Payload);

UCLASS(BlueprintType)
class BLACKMAGICCAMERACONTROL_API UBMCCDispatcher
	: public UObject
	, public IBMCCCallbackHandler
{
	GENERATED_BODY()
public:
	virtual void OnBatteryStatus(const FBMCCBattery_Info& a_BatteryInfo) override;

	UPROPERTY(BlueprintAssignable)
	FOnBatteryStatusReceived BatteryStatusReceived;
};
