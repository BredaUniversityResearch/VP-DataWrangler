// Copyright Epic Games, Inc. All Rights Reserved.

#include "CradleLightControl.h"

#include "AssetToolsModule.h"
#include "LevelEditor.h"
#include "DMXConfigAsset.h"

#include "DesktopPlatformModule.h"
#include "DMXLight.h"
#include "IDesktopPlatform.h"

#include "ToolData.h"
#include "VirtualLight.h"

// Test code for a plugin, mainly trying to get an editor window which can be customized using the Slate Framework
// Don't mind the extra debug-y prints and text pieces

#define LOCTEXT_NAMESPACE "FCradleLightControlModule"

void FCradleLightControlModule::StartupModule()
{
	// This code will execute after your module is loaded into memory; the exact timing is specified in the .uplugin file per-module
	
	VirtualLightToolData = NewObject<UToolData>();
	VirtualLightToolData->ItemClass = UVirtualLight::StaticClass();

	DMXLightToolData = NewObject<UToolData>();
	DMXLightToolData->ItemClass = UDMXLight::StaticClass();

	VirtualLightToolData->AddToRoot();
	DMXLightToolData->AddToRoot();
}

void FCradleLightControlModule::ShutdownModule()
{
	// This function may be called during shutdown to clean up your module.  For modules that support dynamic reloading,
	// we call this function before unloading the module.

	VirtualLightToolData->RemoveFromRoot();
	DMXLightToolData->RemoveFromRoot();

}

FCradleLightControlModule& FCradleLightControlModule::Get()
{
	auto& Module = FModuleManager::GetModuleChecked<FCradleLightControlModule>("CradleLightControl");

	return Module;
}

UToolData* FCradleLightControlModule::GetVirtualLightToolData()
{
	return VirtualLightToolData;
}

UToolData* FCradleLightControlModule::GetDMXLightToolData()
{
	return DMXLightToolData;
}


#undef LOCTEXT_NAMESPACE
	
IMPLEMENT_MODULE(FCradleLightControlModule, CradleLightControl)