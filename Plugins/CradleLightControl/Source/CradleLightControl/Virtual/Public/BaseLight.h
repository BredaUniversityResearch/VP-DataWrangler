#pragma once


#include "BaseLight.generated.h"

UCLASS()
class UBaseLight : public UObject
{
    GENERATED_BODY()

public:
    UBaseLight()
        : Intensity(0.0f)
        , Temperature(0.0f)
        , Hue(0.0f)
        , Saturation(0.0f)
        , Horizontal(0.0f)
        , Vertical(0.0f)
        , InnerAngle(0.0f)
        , OuterAngle(0.0f) {};

    bool IsEnabled() const { return bIsEnabled; };
    float GetIntensity() const { return Intensity; }

    float GetHue() const { return Hue; };
    float GetSaturation() const { return Saturation; };
    bool GetUseTemperature() const { return bUseTemperature; };
    float GetTemperature() const { return Temperature; };

    virtual bool GetCastShadows() const { return false; };

    float GetHorizontal() const { return Horizontal; };
    float GetVertical() const { return Vertical; };
    float GetInnerConeAngle() const { return InnerAngle; };
    float GetOuterConeAngle() const { return OuterAngle; };

    virtual void SetEnabled(bool bNewState);
    virtual void SetLightIntensity(float NormalizedValue);
    virtual void SetHue(float NewValue);
    virtual void SetSaturation(float NewValue);
    virtual void SetUseTemperature(bool NewState);
    virtual void SetTemperature(float NormalizedValue);

    virtual void SetCastShadows(bool bState);

    virtual void AddHorizontal(float Degrees);
    virtual void AddVertical(float Degrees);
    virtual void SetInnerConeAngle(float NewValue);
    virtual void SetOuterConeAngle(float NewValue);

    virtual TSharedPtr<FJsonObject> SaveAsJson() const;
    virtual FPlatformTypes::uint8 LoadFromJson(TSharedPtr<FJsonObject> JsonObject);

    virtual void BeginTransaction();
    virtual void PostTransacted(const FTransactionObjectEvent& TransactionEvent) override;

    FLinearColor GetRGBColor() const;

    UPROPERTY(BlueprintReadOnly)
        bool bIsEnabled;
    UPROPERTY(BlueprintReadOnly)
        float Hue;
    UPROPERTY(BlueprintReadOnly)
        float Saturation;
    UPROPERTY(BlueprintReadOnly)
        float Intensity;

    UPROPERTY(BlueprintReadOnly)
        bool bUseTemperature;
    UPROPERTY(BlueprintReadOnly)
        float Temperature;

    UPROPERTY(BlueprintReadOnly)
        float Horizontal;
    UPROPERTY(BlueprintReadOnly)
        float Vertical;
    UPROPERTY(BlueprintReadOnly)
        float InnerAngle;
    UPROPERTY(BlueprintReadOnly)
        float OuterAngle;
    UPROPERTY()
        bool bLockInnerAngleToOuterAngle;

    // Not UPROPERTY to avoid circular reference with what is essentially shared pointers
    class UItemHandle* Handle;

};
