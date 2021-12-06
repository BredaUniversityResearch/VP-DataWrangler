// Fill out your copyright notice in the Description page of Project Settings.


#include "LightControlBPLib.h"
//#include "Virtual/Public/LightControlBPLib.h"

#include "CradleLightControl.h"
#include "CradleLightControl/DMX/Public/DMXControlTool.h"


UToolData* ULightControlBPLib::GetDMXLightToolData()
{
	auto& Module = FModuleManager::GetModuleChecked<FCradleLightControlModule>("CradleLightControl");

	return Module.DMXControl->GetToolData();
}

UToolData* ULightControlBPLib::GetVirtualLightToolData()
{
	auto& Module = FModuleManager::GetModuleChecked<FCradleLightControlModule>("CradleLightControl");

	return Module.VirtualLightControl->GetToolData();
}
