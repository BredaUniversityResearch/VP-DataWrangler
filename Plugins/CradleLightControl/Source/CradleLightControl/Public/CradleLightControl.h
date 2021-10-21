// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"
#include "LightControlTool.h"

#include "IDetailCustomization.h"

class FCradleLightControlModule : public IModuleInterface
{
public:

	/** IModuleInterface implementation */
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;

	void RegisterTabSpawner();
	void RegisterDMXTabSpawner();

	TSharedPtr<FUICommandList> CommandList;

	TSharedPtr<SDockTab> LightTab;
	TSharedPtr<SDockTab> DMXTab;

	TSharedPtr<SLightControlTool> LightControl;
	TSharedPtr<class SDMXController> DMXControl;


private:

};
