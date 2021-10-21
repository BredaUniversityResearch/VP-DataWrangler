#pragma once



#include "DMXConfigAsset.h"

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
