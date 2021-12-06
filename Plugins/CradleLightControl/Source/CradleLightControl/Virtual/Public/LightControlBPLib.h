// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "Kismet/BlueprintFunctionLibrary.h"
#include "LightControlBPLib.generated.h"

/**
 * 
 */
UCLASS(meta = (ScriptName="CradleLightControl"))
class CRADLELIGHTCONTROL_API ULightControlBPLib : public UBlueprintFunctionLibrary
{
	GENERATED_BODY()

public:
	UFUNCTION(BlueprintPure, Category = "CradleLightControl")
		static class UToolData* GetDMXLightToolData();

	UFUNCTION(BlueprintPure, Category = "CraldeLightControl")
		static class UToolData* GetVirtualLightToolData();

};
