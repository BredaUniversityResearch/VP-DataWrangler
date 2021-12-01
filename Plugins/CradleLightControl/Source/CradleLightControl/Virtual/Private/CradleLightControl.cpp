// Copyright Epic Games, Inc. All Rights Reserved.

#include "CradleLightControl.h"

#include "AssetToolsModule.h"
#include "LevelEditor.h"
#include "LightControlTool.h"
#include "DMXConfigAsset.h"
#include "DMXControlTool.h"

#include "DesktopPlatformModule.h"
#include "IDesktopPlatform.h"

// Test code for a plugin, mainly trying to get an editor window which can be customized using the Slate Framework
// Don't mind the extra debug-y prints and text pieces

#define LOCTEXT_NAMESPACE "FCradleLightControlModule"

void FCradleLightControlModule::StartupModule()
{
	// This code will execute after your module is loaded into memory; the exact timing is specified in the .uplugin file per-module
	
	auto& PropertyModule = FModuleManager::LoadModuleChecked<FPropertyEditorModule>("PropertyEditor");

	PropertyModule.NotifyCustomizationModuleChanged();

	auto& LevelEditorModule = FModuleManager::LoadModuleChecked<FLevelEditorModule>("LevelEditor");
	auto& AssetTools = FModuleManager::LoadModuleChecked<FAssetToolsModule>("AssetTools").Get();



	CommandList = MakeShareable(new FUICommandList);
	// Keeping it here in the scenario that we want to add a button in one of the menus
	//TSharedRef<FExtender> MenuExtender(new FExtender());
	//MenuExtender->AddMenuExtension("EditMain", EExtensionHook::After, CommandList, FMenuExtensionDelegate::CreateLambda(
	//[](FMenuBuilder& MenuBuilder)
	//{
	//		//auto CommandInfo = MakeShareable(new FUICommandInfo());
	//		//MenuBuilder.AddMenuEntry(CommandInfo);
	//}));
	//LevelEditorModule.GetMenuExtensibilityManager()->AddExtender(MenuExtender);
	//auto AssetCategory = AssetTools.RegisterAdvancedAssetCategory("CustomCategory", FText::FromString("Custom Category"));
	auto Action = MakeShared<FDMXConfigAssetAction>();
	//Action.
	
	//AssetToolsModule.Get().
	AssetTools.RegisterAssetTypeActions(Action);

	VirtualLightControl = SNew(SLightControlTool);
	DMXControl = SNew(SDMXControlTool);

	// Create an extension to the toolbar (the one above the viewport in the level editor)
	TSharedRef<FExtender> ToolbarExtender(new FExtender());
	ToolbarExtender->AddToolBarExtension("Settings", EExtensionHook::After, CommandList, FToolBarExtensionDelegate::CreateLambda(
		[this](FToolBarBuilder& MenuBuilder)
		{
			FUIAction Action;
			Action.ExecuteAction = FExecuteAction::CreateLambda([this]()
				{
				    // I could not find a guaranteed, engine provided way to ensure that the button can spawn the tab multiple times
				    // while also not allowing for the tab to be spawned multiple times simultaneously
				    // So we only try to spawn the tab if one doesn't already exist, otherwise we just draw the user's attention to the existing one
                    if (!LightTab)
                    {
					    RegisterTabSpawner();
					    FGlobalTabmanager::Get()->TryInvokeTab(FTabId("VirtualLightControl"));                        
                    }
					else
					    LightTab->DrawAttention();

                    if (!DMXTab)
                    {
						RegisterDMXTabSpawner();
						FGlobalTabmanager::Get()->TryInvokeTab(FTabId("DMXControl"));

                    }
				});
			MenuBuilder.AddToolBarButton(Action, NAME_None, FText::FromString("Cradle Light Control"));
		}));

    LevelEditorModule.GetToolBarExtensibilityManager()->AddExtender(ToolbarExtender);

	FCoreDelegates::OnEnginePreExit.AddLambda([this]()
		{
			if (VirtualLightControl)
				VirtualLightControl->PreDestroy();
			if (DMXControl)
				DMXControl->PreDestroy();

			VirtualLightControl.Reset();
			DMXControl.Reset();
		});

	// Ensure that slate throttling is disabled
	// If it is enabled, interacting with any slate widgets will freeze the main viewport
	// until the interaction is finished. This makes editing light properties more cumbersome if not disabled.
	IConsoleManager::Get().FindConsoleVariable(TEXT("Slate.bAllowThrottling"))->Set(false);

}

void FCradleLightControlModule::ShutdownModule()
{
	// This function may be called during shutdown to clean up your module.  For modules that support dynamic reloading,
	// we call this function before unloading the module.
	if (VirtualLightControl)
		VirtualLightControl->PreDestroy();

	if (DMXControl)
		DMXControl->PreDestroy();

}


bool FCradleLightControlModule::OpenFileDialog(FString Title, void* NativeWindowHandle, FString DefaultPath, uint32 Flags,
	FString FileTypeList, TArray<FString>& OutFilenames)
{
	IDesktopPlatform* Platform = FDesktopPlatformModule::Get();
	return Platform->OpenFileDialog(NativeWindowHandle, Title, DefaultPath, "", FileTypeList, Flags, OutFilenames);
}

bool FCradleLightControlModule::SaveFileDialog(FString Title, void* NativeWindowHandle, FString DefaultPath, uint32 Flags,
                                               FString FileTypeList, TArray<FString>& OutFilenames)
{
	IDesktopPlatform* Platform = FDesktopPlatformModule::Get();
	return Platform->SaveFileDialog(NativeWindowHandle, Title, DefaultPath, "", FileTypeList, Flags, OutFilenames);
}

void FCradleLightControlModule::OpenGelPalette(FGelPaletteSelectionCallback SelectionCallback)
{
	if (!GelPalette)
	{
		GelPalette = SNew(SGelPaletteWidget);
	}
	
	if (!GelPaletteWindow)
	{

		GelPaletteWindow = SNew(SWindow)
			.ClientSize(FVector2D(640.0f, 480.0f))
			.Title(FText::FromString("Light Gel Palette"))
			.CreateTitleBar(true)
			[
				GelPalette->AsShared()
			];
		GelPaletteWindow = FSlateApplication::Get().AddWindow(GelPaletteWindow.ToSharedRef());
			//.IsPopupWindow(true)
			
		GelPalette->Window = GelPaletteWindow;

		//GelPaletteWindow->ShowWindow();
	}

	if (!GelPaletteWindow->IsVisible())
	{
		GelPalette->SelectionCallback = SelectionCallback;
		GelPaletteWindow->ShowWindow();		
	}
	else
	{
		GelPalette->SelectionCallback = SelectionCallback;
		GelPaletteWindow->FlashWindow();
		//GelPaletteWindow->DrawAttention()
	}

	
}

void FCradleLightControlModule::CloseGelPalette()
{
	GelPaletteWindow->HideWindow();
}

void FCradleLightControlModule::RegisterTabSpawner()
{
	FGlobalTabmanager::Get()->RegisterNomadTabSpawner("VirtualLightControl", FOnSpawnTab::CreateLambda([this](const FSpawnTabArgs& Args)
		{
			/*LightTab = SNew(SDockTab)
				.Label(FText::FromString("Light control tab"))
				.TabRole(ETabRole::NomadTab)
				.OnTabClosed_Lambda([this](TSharedRef<SDockTab>)
					{
						return;
						FGlobalTabmanager::Get()->UnregisterNomadTabSpawner("VirtualLightControl");
						VirtualLightControl->PreDestroy();
						VirtualLightControl.Reset();
						LightTab.Reset();
					});

			LightTab->SetContent(				    
				    SAssignNew(VirtualLightControl, SLightControlTool)
				    .ToolTab(LightTab)
					
				);*/

		    return VirtualLightControl->Show();
			
		}));
}

void FCradleLightControlModule::RegisterDMXTabSpawner()
{

	FGlobalTabmanager::Get()->RegisterNomadTabSpawner("DMXControl", FOnSpawnTab::CreateLambda([this](const FSpawnTabArgs& Args)
		{
			//DMXTab = SNew(SDockTab)
			//	.Label(FText::FromString("DMX control tab"))
			//	.TabRole(ETabRole::NomadTab)
			//	.OnTabClosed_Lambda([this](TSharedRef<SDockTab>)
			//		{
			//			FGlobalTabmanager::Get()->UnregisterNomadTabSpawner("DMXControl");
			//			//DMXControl->PreDestroy();
			//			DMXControl.Reset();
			//			DMXTab.Reset();
			//		});

			//DMXTab->SetContent(
			//	SAssignNew(DMXControl, SDMXControlTool)
			//	.ToolTab(DMXTab)
			//);

			return DMXControl->Show();

		}));

}


#undef LOCTEXT_NAMESPACE
	
IMPLEMENT_MODULE(FCradleLightControlModule, CradleLightControl)