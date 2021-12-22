// Copyright Epic Games, Inc. All Rights Reserved.

#pragma once

#include "CoreMinimal.h"
#include "Modules/ModuleManager.h"
#include "LightControlTool.h"

#include "GelPaletteWidget.h"

#include "IDetailCustomization.h"
#include "Chaos/AABB.h"


UENUM()
enum EIconType
{
	GeneralLightOff = 0,
	GeneralLightOn,
	GeneralLightUndetermined,
	SkyLightOff,
	SkyLightOn,
	SkyLightUndetermined,
	SpotLightOff,
	SpotLightOn,
	SpotLightUndetermined,
	DirectionalLightOff,
	DirectionalLightOn,
	DirectionalLightUndetermined,
	PointLightOff,
	PointLightOn,
	PointLightUndetermined,
	FolderClosed,
	FolderOpened
};

class UItemHandle;

class FCradleLightControlEditorModule : public IModuleInterface
{
public:

	/** IModuleInterface implementation */
	virtual void StartupModule() override;
	virtual void ShutdownModule() override;


	static bool OpenFileDialog(FString Title, void*
		NativeWindowHandle, FString DefaultPath, uint32 Flags, FString FileTypeList, TArray<FString>& OutFilenames);
	static bool SaveFileDialog(FString Title, void*
		NativeWindowHandle, FString DefaultPath, uint32 Flags, FString FileTypeList, TArray<FString>& OutFilenames);

	static FCradleLightControlEditorModule& Get();

	void OpenGelPalette(FGelPaletteSelectionCallback SelectionCallback);
	void CloseGelPalette();

	void RegisterTabSpawner();
	void RegisterDMXTabSpawner();

	void GenerateItemHandleWidget(UItemHandle* ItemHandle);

	void GenerateIcons();
	FCheckBoxStyle MakeCheckboxStyleForType(uint8 IconType);
	FSlateBrush& GetIcon(EIconType Icon);



	TSharedPtr<FUICommandList> CommandList;

	TSharedPtr<SDockTab> LightTab;
	TSharedPtr<SDockTab> DMXTab;

	TSharedPtr<SLightControlTool> VirtualLightControl;
	TSharedPtr<class SDMXControlTool> DMXControl;

	TSharedPtr<SGelPaletteWidget> GelPalette;
	TSharedPtr<SWindow> GelPaletteWindow;


	TMap<TEnumAsByte<EIconType>, FSlateBrush> Icons;
private:

};
