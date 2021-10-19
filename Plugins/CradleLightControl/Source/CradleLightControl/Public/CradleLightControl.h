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

	TSharedPtr<FUICommandList> CommandList;

	TSharedPtr<SDockTab> DockTab;

	TSharedPtr<SLightControlTool> LightControl;


private:

};
