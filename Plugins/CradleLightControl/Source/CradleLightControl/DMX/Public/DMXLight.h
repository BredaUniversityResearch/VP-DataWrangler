#pragma once

#include "CoreMinimal.h"

#include "DMXConfigAsset.h"

#include "DMXLight.generated.h"

UCLASS(BlueprintType)
class UDMXLight : public UObject
{
public:
    GENERATED_BODY()
    UDMXLight()
        : Name("New DMX Light")
    {};

    UPROPERTY(EditAnywhere)
        FString Name;

    UPROPERTY(EditAnywhere)
        UDMXConfigAsset* Config;

    UPROPERTY(BlueprintReadOnly)
        float Horizontal;

    UPROPERTY(BlueprintReadOnly)
        float Vertical;

    

};