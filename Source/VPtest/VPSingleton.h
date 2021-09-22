// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "UObject/NoExportTypes.h"
#include "VPSingleton.generated.h"

/**
 * Singleton acting as global storage for variables which would never make sense differing across actors.
 */
UCLASS(BluePrintable)
class VPTEST_API UVPSingleton : public UObject
{
    GENERATED_BODY()

public:

    

    UPROPERTY(EditDefaultsOnly, BlueprintReadOnly)
    FTransform RoomCorrection;
	
};
