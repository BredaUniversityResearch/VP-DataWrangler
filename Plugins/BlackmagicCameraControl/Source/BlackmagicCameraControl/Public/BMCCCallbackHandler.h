#pragma once

#include "BMCCCallbackHandler.generated.h"

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
	virtual void OnBatteryStatus(const FBMCCBattery_Info& a_BatteryInfo) = 0;
};
