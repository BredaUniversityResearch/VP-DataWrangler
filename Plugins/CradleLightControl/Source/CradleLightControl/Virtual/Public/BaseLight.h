#pragma once
#include "Chaos/AABB.h"
#include "Chaos/AABB.h"


#include "BaseLight.generated.h"

UCLASS(BlueprintType)
class UBaseLight : public UObject
{
    GENERATED_BODY()

public:
    UBaseLight()
        : Intensity(0.0f)
        , Hue(0.0f)
        , Saturation(0.0f)
        , Temperature(0.0f)
        , Horizontal(0.0f)
        , Vertical(0.0f)
        , InnerAngle(0.0f)
        , OuterAngle(0.0f)
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

    UFUNCTION(BlueprintCallable)
    virtual void SetEnabled(bool bNewState);
    UFUNCTION(BlueprintCallable)
        virtual void SetLightIntensity(float NormalizedValue);
    UFUNCTION(BlueprintCallable)
        virtual void SetLightIntensityRaw(float Value);
    UFUNCTION(BlueprintCallable)
        virtual void SetHue(float NewValue);
    UFUNCTION(BlueprintCallable)
        virtual void SetSaturation(float NewValue);
    UFUNCTION(BlueprintCallable)
        virtual void SetUseTemperature(bool NewState);
    UFUNCTION(BlueprintCallable)
        virtual void SetTemperature(float NormalizedValue);
    UFUNCTION(BlueprintCallable)
        virtual void SetTemperatureRaw(float Value);


    UFUNCTION(BlueprintCallable)
        virtual void SetCastShadows(bool bState);

    UFUNCTION(BlueprintCallable)
        virtual void AddHorizontal(float NormalizedDegrees);
    UFUNCTION(BlueprintCallable)
        virtual void AddVertical(float NormalizedDegrees);
    UFUNCTION(BlueprintCallable)
        virtual void SetInnerConeAngle(float NewValue);
    UFUNCTION(BlueprintCallable)
        virtual void SetOuterConeAngle(float NewValue);

    virtual TSharedPtr<FJsonObject> SaveAsJson();
    virtual FPlatformTypes::uint8 LoadFromJson(TSharedPtr<FJsonObject> JsonObject);

    virtual void BeginTransaction();
    virtual void PostTransacted(const FTransactionObjectEvent& TransactionEvent) override;

    FLinearColor GetRGBColor() const;




    UPROPERTY(BlueprintReadOnly)
        bool bIsEnabled;

    UPROPERTY(BlueprintReadOnly)
        float Intensity;
    UPROPERTY(BlueprintReadOnly)
        float Hue;
    UPROPERTY(BlueprintReadOnly)
        float Saturation;
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
