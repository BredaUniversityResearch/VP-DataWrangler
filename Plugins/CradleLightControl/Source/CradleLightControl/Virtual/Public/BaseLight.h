#pragma once
#include "Chaos/AABB.h"
#include "Chaos/AABB.h"


#include "BaseLight.generated.h"

UCLASS()
class UBaseLight : public UObject
{
    GENERATED_BODY()

public:
    UBaseLight()
    {
        SetFlags(GetFlags() | RF_Transactional);
    };

    bool IsEnabled() const { return bIsEnabled; };

    virtual float GetIntensityNormalized() const { return Intensity; }
    virtual float GetHue() const { return Hue; };
    virtual float GetSaturation() const { return Saturation; };
    virtual bool GetUseTemperature() const { return bUseTemperature; };
    virtual float GetTemperatureNormalized() const { return Temperature; };

    virtual bool GetCastShadows() const { return false; };

    virtual float GetHorizontalNormalized() const { return Horizontal; };
    virtual float GetVerticalNormalized() const { return Vertical; };
    virtual float GetInnerConeAngle() const { return InnerAngle; };
    virtual float GetOuterConeAngle() const { return OuterAngle; };

    virtual void SetEnabled(bool bNewState);
    virtual void SetLightIntensity(float NormalizedValue);
    virtual void SetLightIntensityRaw(float Value);
    virtual void SetHue(float NewValue);
    virtual void SetSaturation(float NewValue);
    virtual void SetUseTemperature(bool NewState);
    virtual void SetTemperature(float NormalizedValue);
    virtual void SetTemperatureRaw(float Value);


    virtual void SetCastShadows(bool bState);

    virtual void AddHorizontal(float NormalizedDegrees);
    virtual void AddVertical(float NormalizedDegrees);
    virtual void SetInnerConeAngle(float NewValue);
    virtual void SetOuterConeAngle(float NewValue);

    virtual TSharedPtr<FJsonObject> SaveAsJson();
    virtual FPlatformTypes::uint8 LoadFromJson(TSharedPtr<FJsonObject> JsonObject);

    virtual void BeginTransaction();
    virtual void PostTransacted(const FTransactionObjectEvent& TransactionEvent) override;

    FLinearColor GetRGBColor() const;




    UPROPERTY(BlueprintReadOnly)
        bool bIsEnabled{ false };

    UPROPERTY(BlueprintReadOnly)
        float Intensity{ 0.0f };
    UPROPERTY(BlueprintReadOnly)
        float Hue{ 0.0f };
    UPROPERTY(BlueprintReadOnly)
        float Saturation{ 0.0f };
    UPROPERTY(BlueprintReadOnly)
        bool bUseTemperature{ 0.0f };
    UPROPERTY(BlueprintReadOnly)
        float Temperature{ 0.0f };

    UPROPERTY(BlueprintReadOnly)
        float Horizontal{ 0.0f };
    UPROPERTY(BlueprintReadOnly)
        float Vertical{ 0.0f };
    UPROPERTY(BlueprintReadOnly)
        float InnerAngle{ 0.0f };
    UPROPERTY(BlueprintReadOnly)
        float OuterAngle{ 0.0f };
    UPROPERTY()
        bool bLockInnerAngleToOuterAngle{ false };

    // Not UPROPERTY to avoid circular reference with what is essentially shared pointers
    class UItemHandle* Handle;

};
