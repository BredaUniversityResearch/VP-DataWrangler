// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"
//#include "LightControlTool.h"

//#include "GelPaletteWidget.h"

#include "IDetailCustomization.h"
#include "Chaos/AABB.h"

class UToolData;

class CRADLELIGHTCONTROL_API FCradleLightControlModule : public IModuleInterface
{
public:

	/** IModuleInterface implementation */
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;

	static FCradleLightControlModule& Get();

	UToolData* GetVirtualLightToolData();
	UToolData* GetDMXLightToolData();


private:

	UToolData* VirtualLightToolData;
	UToolData* DMXLightToolData;
};
