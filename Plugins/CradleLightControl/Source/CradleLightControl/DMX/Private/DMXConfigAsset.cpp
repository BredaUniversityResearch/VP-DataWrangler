#pragma once



#include "../Public/DMXConfigAsset.h"

#include "DMXLight.h"

float FDMXChannel::NormalizedToValue(float Normalized)
{
    return Normalized * GetValueRange() + MinValue;
}

uint8 FDMXChannel::NormalizedToDMX(float Normalized)
{
    return StaticCast<uint8>(Normalized * GetDMXValueRange() + MinDMXValue);
}

float FDMXChannel::NormalizeValue(float Value)
{
    return (Value - MinValue) / GetValueRange();
}

uint8 FDMXChannel::ValueToDMX(float Value)
{
    return NormalizedToDMX(NormalizeValue(Value));
}

void FDMXChannel::SetChannel(TMap<int32, uint8>& Channels, float Value, int32 StartingChannel)
{
	if (bEnabled)
	{
        // We subtract 1 to make it more intuitive - first channel in config means starting channel for the light
        Channels.FindOrAdd(StartingChannel + Channel - 1) = ValueToDMX(Value);
		
	}
}

float FDMXChannel::GetValueRange() const
{
    return MaxValue - MinValue;
}

uint8 FDMXChannel::GetDMXValueRange() const
{
    return MaxDMXValue - MinDMXValue;
}

FText FDMXConfigAssetAction::GetName() const
{
    return FText::FromString("DMX Light Config");
}

FColor FDMXConfigAssetAction::GetTypeColor() const
{
    return FColor::Cyan;
}

uint32 FDMXConfigAssetAction::GetCategories()
{
    return EAssetTypeCategories::Blueprint;
}

UClass* FDMXConfigAssetAction::GetSupportedClass() const
{
    return UDMXConfigAsset::StaticClass();
}

void FDMXConfigAssetAction::OpenAssetEditor(const TArray<UObject*>& InObjects,
    TSharedPtr<IToolkitHost> EditWithinLevelEditor)
{
    FSimpleAssetEditor::CreateEditor(EToolkitMode::Standalone, EditWithinLevelEditor, InObjects);


}

void UDMXConfigAsset::SetChannels(UDMXLight* DMXLight, TMap<int32, uint8>& Channels)
{
    auto RGB = DMXLight->GetRGBColor();

    for (auto Constant : ConstantChannels)
    {
        Channels.FindOrAdd(Constant.Channel) = Constant.Value;
    }

    HorizontalChannel.SetChannel(Channels, DMXLight->Horizontal, DMXLight->StartingChannel);
    VerticalChannel.SetChannel(Channels, DMXLight->Vertical, DMXLight->StartingChannel);

    IntensityChannel.SetChannel(Channels, DMXLight->Intensity, DMXLight->StartingChannel);

    RedChannel.SetChannel(Channels, RGB.R, DMXLight->StartingChannel);
    GreenChannel.SetChannel(Channels, RGB.G, DMXLight->StartingChannel);
    BlueChannel.SetChannel(Channels, RGB.B, DMXLight->StartingChannel);

    OnOffChannel.SetChannel(Channels, DMXLight->bIsEnabled ? 1.0f : 0.0f, DMXLight->StartingChannel);

}
