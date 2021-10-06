#pragma once

#include "Slate.h"

class SLightPropertyEditor : public SCompoundWidget
{
public:
    SLATE_BEGIN_ARGS(SLightPropertyEditor) {}

    SLATE_ARGUMENT(class SLightControlTool*, CoreToolPtr)

    SLATE_END_ARGS();

    static TArray<FColor> LinearGradient(TArray<FColor> ControlPoints, FVector2D Size = FVector2D(1.0f, 256.0f), EOrientation Orientation = Orient_Vertical);

    static UTexture2D* MakeGradientTexture(int X = 1, int Y = 256);


    void Construct(const FArguments& Args);
    void PreDestroy();
    void FinishInit();

    void GenerateTextures();
    void UpdateSaturationGradient(float NewHue);
    const FSlateBrush* GetSaturationGradientBrush() const;

    void OnIntensityValueChanged(float Value);
    FText GetIntensityValueText() const;
    float GetIntensityValue() const;
    FText GetIntensityPercentage() const;

    void OnHueValueChanged(float Value);
    FText GetHueValueText() const;
    float GetHueValue() const;
    FText GetHuePercentage() const;

    void OnSaturationValueChanged(float Value);
    FText GetSaturationValueText() const;
    float GetSaturationValue() const;


    class SLightControlTool* CoreToolPtr;
    TWeakPtr<class SLightTreeHierarchy> TreeWidget;


    TSharedPtr<FSlateImageBrush> IntensityGradientBrush;
    TSharedPtr<FSlateImageBrush> HSVGradientBrush;
    TSharedPtr<FSlateImageBrush> DefaultSaturationGradientBrush;
    TSharedPtr<FSlateImageBrush> SaturationGradientBrush;
    TSharedPtr<FSlateImageBrush> TemperatureGradientBrush;

    UPROPERTY()
        UTexture2D* IntensityGradientTexture;
    UPROPERTY()
        UTexture2D* HSVGradientTexture;
    UPROPERTY()
        UTexture2D* DefaultSaturationGradientTexture;
    UPROPERTY()
        UTexture2D* SaturationGradientTexture;
    UPROPERTY()
        UTexture2D* TemperatureGradientTexture;
};