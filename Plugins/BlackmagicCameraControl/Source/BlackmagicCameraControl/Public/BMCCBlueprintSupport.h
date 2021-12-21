#pragma once

#include "BMCCBlueprintSupport.generated.h"

class UBMCCDispatcher;
UCLASS(BlueprintType)
class UBMCCBlueprintSupport: public UBlueprintFunctionLibrary
{
	GENERATED_BODY()
public:
	UFUNCTION(BlueprintCallable)
	static UBMCCDispatcher* GetBlackmagicCameraControlDispatcher();
};