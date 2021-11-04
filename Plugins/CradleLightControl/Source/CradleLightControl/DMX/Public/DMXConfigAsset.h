#pragma once

#include "AssetTypeActions_Base.h"

#include "CoreMinimal.h"


#include "Chaos/AABB.h"
#include "Chaos/AABB.h"
#include "UObject/NoExportTypes.h"

#include "DMXConfigAsset.generated.h"

USTRUCT(BlueprintType)
struct CRADLELIGHTCONTROL_API FDMXChannel
{
    GENERATED_BODY()

    FDMXChannel()
	    : MinValue(0.0f)
		, MaxValue(100.0f)
		, MinDMXValue(0)
		, MaxDMXValue(255)
		, bEnabled(true)
		, Channel(1) {}


    //UFUNCTION(BlueprintCallable)
    float NormalizedToValue(float Normalized);

    //UFUNCTION(BlueprintCallable)
    uint8 NormalizedToDMX(float Normalized);

    //UFUNCTION(BlueprintCallable)
    float NormalizeValue(float Value);

    uint8 ValueToDMX(float Value);

    void SetChannel(TMap<int32, uint8>& Channels, float Value, int32 StartingChannel);

    float GetValueRange() const;
    uint8 GetDMXValueRange() const;

    UPROPERTY(EditAnywhere)
        bool bEnabled;

    UPROPERTY(EditAnywhere)
        int32 Channel;

    UPROPERTY(EditAnywhere)
        uint8 MinDMXValue;
    UPROPERTY(EditAnywhere)
        uint8 MaxDMXValue;

    UPROPERTY(EditAnywhere)
        float MinValue;
    UPROPERTY(EditAnywhere)
        float MaxValue;
    
    
};


USTRUCT(BlueprintType)
struct CRADLELIGHTCONTROL_API FConstDMXChannel
{
    GENERATED_BODY()

    UPROPERTY(EditAnywhere)
        int32 Channel;

    UPROPERTY(EditAnywhere)
        uint8 Value;
};

class FDMXConfigAssetAction : public FAssetTypeActions_Base
{
    virtual FText GetName() const override;
    virtual FColor GetTypeColor() const override;
    virtual uint32 GetCategories() override;
    virtual UClass* GetSupportedClass() const override;
    virtual void OpenAssetEditor(const TArray<UObject*>& InObjects, TSharedPtr<IToolkitHost> EditWithinLevelEditor) override;
};

UCLASS(BlueprintType)
class CRADLELIGHTCONTROL_API UDMXConfigAsset : public UObject
{
    GENERATED_BODY()

public:

    void SetChannels(class UDMXLight* DMXLight, TMap<int32, uint8>& Channels);

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
        FDMXChannel OnOffChannel;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
        FDMXChannel HorizontalChannel;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
        FDMXChannel VerticalChannel;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
        FDMXChannel RedChannel;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
        FDMXChannel GreenChannel;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
        FDMXChannel BlueChannel;

    UPROPERTY(EditAnywhere, BlueprintReadWrite)
        FDMXChannel IntensityChannel;

    UPROPERTY(EditAnywhere, BlueprintReadOnly)
		TArray<FConstDMXChannel> ConstantChannels;
};