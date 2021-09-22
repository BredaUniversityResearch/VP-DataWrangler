// Copyright Epic Games, Inc. All Rights Reserved.

#include "CradleLightControl.h"

#include "CentralLightController.h"
#include "LevelEditor.h"
#include "LightControlDetail.h"
#include "LightControlTool.h"


// Test code for a plugin, mainly trying to get an editor window which can be customized using the Slate Framework
// Don't mind the extra debug-y prints and text pieces

#define LOCTEXT_NAMESPACE "FCradleLightControlModule"

void FCradleLightControlModule::StartupModule()
{
	// This code will execute after your module is loaded into memory; the exact timing is specified in the .uplugin file per-module
	
	auto& PropertyModule = FModuleManager::LoadModuleChecked<FPropertyEditorModule>("PropertyEditor");

	PropertyModule.RegisterCustomClassLayout("CentralLightController", FOnGetDetailCustomizationInstance::CreateStatic(FLightControlDetailCustomization::MakeInstance));

	PropertyModule.NotifyCustomizationModuleChanged();

	auto& LevelEditorModule = FModuleManager::LoadModuleChecked<FLevelEditorModule>("LevelEditor");

	CommandList = MakeShareable(new FUICommandList);
	TSharedRef<FExtender> MenuExtender(new FExtender());
	MenuExtender->AddMenuExtension("EditMain", EExtensionHook::After, CommandList, FMenuExtensionDelegate::CreateLambda(
	[](FMenuBuilder& MenuBuilder)
	{
			GEngine->AddOnScreenDebugMessage(-1, 300.0f, FColor::Black, "Menu Extension lel");

			//auto CommandInfo = MakeShareable(new FUICommandInfo());
			//MenuBuilder.AddMenuEntry(CommandInfo);
	}));


	TSharedRef<FExtender> ToolbarExtender(new FExtender());
	ToolbarExtender->AddToolBarExtension("Game", EExtensionHook::After, CommandList, FToolBarExtensionDelegate::CreateLambda(
		[](FToolBarBuilder& MenuBuilder)
		{
			GEngine->AddOnScreenDebugMessage(-1, 300.0f, FColor::Black, "Toolbar Extension lel");

			//auto Action = MakeShared<FUIAction>();
			//TSharedPtr<FUIAction> Action = MakeShareable(new FUIAction);
			FUIAction Action;
			Action.ExecuteAction = FExecuteAction::CreateLambda([]()
				{
					GEngine->AddOnScreenDebugMessage(-1, 300.0f, FColor::Black, "Clickity clackity this button is my property");
					ULightControlTool* Tool = NewObject<ULightControlTool>(GetTransientPackage(), ULightControlTool::StaticClass());

					Tool->AddToRoot();

					FPropertyEditorModule& PropertyModule = FModuleManager::LoadModuleChecked<FPropertyEditorModule>("PropertyEditor");

					TArray<UObject*> ObjectsToView;
					ObjectsToView.Add(Tool);
					auto Window = PropertyModule.CreateFloatingDetailsView(ObjectsToView, false);


					Window->SetOnWindowClosed(FOnWindowClosed::CreateLambda([Tool](const TSharedRef<SWindow> Window)
						{
							Tool->RemoveFromRoot();
						}));

					Window->AddOverlaySlot()
						.HAlign(EHorizontalAlignment::HAlign_Fill)
						.VAlign(EVerticalAlignment::VAlign_Fill)
						[
							SNew(STextBlock)
							.Text(FText::FromString("Eyyyy we got it"))
				        ];

					Window->GetContent();
				});
			MenuBuilder.AddToolBarButton(Action);
		}));



	LevelEditorModule.GetMenuExtensibilityManager()->AddExtender(MenuExtender);


    LevelEditorModule.GetToolBarExtensibilityManager()->AddExtender(ToolbarExtender);

}

void FCradleLightControlModule::ShutdownModule()
{
	auto& PropertyModule = FModuleManager::LoadModuleChecked<FPropertyEditorModule>("PropertyEditor");

	PropertyModule.UnregisterCustomClassLayout("LightControlTool");
	PropertyModule.NotifyCustomizationModuleChanged();

	// This function may be called during shutdown to clean up your module.  For modules that support dynamic reloading,
	// we call this function before unloading the module.
}

#undef LOCTEXT_NAMESPACE
	
IMPLEMENT_MODULE(FCradleLightControlModule, CradleLightControl)