#pragma once

#include "AssetTypeActions_Base.h"

#include "CoreMinimal.h"
#include "UObject/NoExportTypes.h"

#include "DMXConfigAsset.generated.h"

USTRUCT(BlueprintType)
struct CRADLELIGHTCONTROL_API FDMXChannel
{
    GENERATED_BODY()
    UPROPERTY(EditAnywhere)
        bool bEnabled;

    UPROPERTY(EditAnywhere)
        int32 Channel;

    UPROPERTY(EditAnywhere)
        uint8 MinimumDMXValue;
    UPROPERTY(EditAnywhere)
        uint8 MaximumDMXValue;

    UPROPERTY(EditAnywhere)
        int32 MinSliderValue;
    UPROPERTY(EditAnywhere)
        int32 MaxSliderValue;

    UPROPERTY(EditAnywhere)
    class UDMXConfigAsset* Test;
    
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
    UPROPERTY(EditAnywhere, BlueprintReadWrite)
        FDMXChannel Horizontal;
};