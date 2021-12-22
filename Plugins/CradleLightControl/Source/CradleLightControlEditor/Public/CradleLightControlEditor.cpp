// Copyright Epic Games, Inc. All Rights Reserved.

#include "CradleLightControlEditor.h"

#include "AssetToolsModule.h"
#include "LevelEditor.h"
#include "LightControlTool.h"
#include "DMXConfigAsset.h"
#include "DMXControlTool.h"

#include "ItemHandle.h"

#include "ClassIconFinder.h"
#include "CradleLightControl.h"
#include "Engine/DirectionalLight.h"
#include "Engine/PointLight.h"
#include "Engine/SkyLight.h"
#include "Engine/SpotLight.h"

#include "DesktopPlatformModule.h"
#include "IDesktopPlatform.h"

// Test code for a plugin, mainly trying to get an editor window which can be customized using the Slate Framework
// Don't mind the extra debug-y prints and text pieces

#define LOCTEXT_NAMESPACE "FCradleLightControlEditorModule"

void FCradleLightControlEditorModule::StartupModule()
{
	// This code will execute after your module is loaded into memory; the exact timing is specified in the .uplugin file per-module

	auto& PropertyModule = FModuleManager::LoadModuleChecked<FPropertyEditorModule>("PropertyEditor");

	PropertyModule.NotifyCustomizationModuleChanged();

	auto& LevelEditorModule = FModuleManager::LoadModuleChecked<FLevelEditorModule>("LevelEditor");
	auto& AssetTools = FModuleManager::LoadModuleChecked<FAssetToolsModule>("AssetTools").Get();

	GenerateIcons();



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

	VirtualLightControl = SNew(SLightControlTool, FCradleLightControlModule::Get().GetVirtualLightToolData());
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
	//IConsoleManager::Get().FindConsoleVariable(TEXT("Slate.bAllowThrottling"))->Set(false);

}

void FCradleLightControlEditorModule::ShutdownModule()
{
	// This function may be called during shutdown to clean up your module.  For modules that support dynamic reloading,
	// we call this function before unloading the module.
	if (VirtualLightControl)
		VirtualLightControl->PreDestroy();

	if (DMXControl)
		DMXControl->PreDestroy();

}


bool FCradleLightControlEditorModule::OpenFileDialog(FString Title, void* NativeWindowHandle, FString DefaultPath, uint32 Flags,
	FString FileTypeList, TArray<FString>& OutFilenames)
{
	IDesktopPlatform* Platform = FDesktopPlatformModule::Get();
	return Platform->OpenFileDialog(NativeWindowHandle, Title, DefaultPath, "", FileTypeList, Flags, OutFilenames);
}

bool FCradleLightControlEditorModule::SaveFileDialog(FString Title, void* NativeWindowHandle, FString DefaultPath, uint32 Flags,
	FString FileTypeList, TArray<FString>& OutFilenames)
{
	IDesktopPlatform* Platform = FDesktopPlatformModule::Get();
	return Platform->SaveFileDialog(NativeWindowHandle, Title, DefaultPath, "", FileTypeList, Flags, OutFilenames);
}

FCradleLightControlEditorModule& FCradleLightControlEditorModule::Get()
{
	auto& Module = FModuleManager::GetModuleChecked<FCradleLightControlEditorModule>("CradleLightControlEditor");
	return Module;
}

void FCradleLightControlEditorModule::OpenGelPalette(FGelPaletteSelectionCallback SelectionCallback)
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

void FCradleLightControlEditorModule::CloseGelPalette()
{
	GelPaletteWindow->HideWindow();
}

void FCradleLightControlEditorModule::RegisterTabSpawner()
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

void FCradleLightControlEditorModule::RegisterDMXTabSpawner()
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

void FCradleLightControlEditorModule::GenerateItemHandleWidget(UItemHandle* ItemHandle)
{
	if (!ItemHandle->TableRowBox)
		SAssignNew(ItemHandle->TableRowBox, SBox);
	auto IconType = ItemHandle->Type;
	if (ItemHandle->Type == Folder)
	{
		if (ItemHandle->Children.Num())
		{
			IconType = ItemHandle->Children[0]->Type; // This is 0 if there is a folder as the first child, which leads to out of bounds indexing
			for (size_t i = 1; i < ItemHandle->Children.Num(); i++)
			{
				if (IconType != ItemHandle->Children[i]->Type)
				{
					IconType = Mixed;
				}
			}
		}
		else
			IconType = Mixed;
	}

	ItemHandle->CheckBoxStyle = MakeCheckboxStyleForType(IconType);

	ItemHandle->CheckBoxStyle.CheckedPressedImage = ItemHandle->CheckBoxStyle.UndeterminedImage;
	ItemHandle->CheckBoxStyle.UncheckedPressedImage = ItemHandle->CheckBoxStyle.UndeterminedImage;

	SHorizontalBox::FSlot* CheckBoxSlot;


	if (ItemHandle->Type != Folder)
	{
		SHorizontalBox::FSlot* TextSlot;
		ItemHandle->TableRowBox->SetContent(
			SNew(SHorizontalBox)
			+ SHorizontalBox::Slot()
			.Expose(CheckBoxSlot) // On/Off toggle button 
			[
				SNew(SCheckBox)
				.IsChecked_UObject(ItemHandle, &UItemHandle::IsLightEnabled)
				.OnCheckStateChanged_UObject(ItemHandle, &UItemHandle::OnCheck)
				.Style(&ItemHandle->CheckBoxStyle)
			]
			+ SHorizontalBox::Slot() // Name slot
			.Expose(TextSlot)
			.VAlign(VAlign_Center)
			[
				SAssignNew(ItemHandle->RowNameBox, SBox)
			]
			+ SHorizontalBox::Slot()
			.Padding(10.0f, 0.0f, 0.0f, 3.0f)
			.VAlign(VAlign_Bottom)
			[
				SNew(STextBlock)
				.Text(FText::FromString(ItemHandle->Note))
			]
		);

		TextSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
	}
	else
	{
		SHorizontalBox::FSlot* FolderImageSlot;
		SHorizontalBox::FSlot* CloseButtonSlot;
		ItemHandle->TableRowBox->SetContent(
			SNew(SHorizontalBox)
			+ SHorizontalBox::Slot() // Name slot
			.VAlign(VAlign_Center)
			[
				SAssignNew(ItemHandle->RowNameBox, SBox)
			]
		+ SHorizontalBox::Slot()
			.Expose(CloseButtonSlot)
			.HAlign(HAlign_Right)
			[
				SNew(SButton)
				.Text(FText::FromString("Delete"))
			.OnClicked_UObject(ItemHandle, &UItemHandle::RemoveFromTree)
			]
			+ SHorizontalBox::Slot() // On/Off toggle button
			.Expose(CheckBoxSlot)
			.HAlign(HAlign_Right)
			[
				SAssignNew(ItemHandle->StateCheckbox, SCheckBox)
				.IsChecked_UObject(ItemHandle, &UItemHandle::IsLightEnabled)
				.OnCheckStateChanged_UObject(ItemHandle, &UItemHandle::OnCheck)
				.Style(&ItemHandle->CheckBoxStyle)
				.RenderTransform(FSlateRenderTransform(FScale2D(1.1f)))
			]
			+ SHorizontalBox::Slot()
			.Expose(FolderImageSlot)
			.HAlign(HAlign_Right)
			.Padding(3.0f, 0.0f, 3.0f, 0.0f)
			[
				SNew(SButton)
				.ButtonColorAndOpacity(FSlateColor(FColor::Transparent))
				.OnClicked_Lambda([ItemHandle]()
					{
						ItemHandle->bExpanded = !ItemHandle->bExpanded;
						ItemHandle->ExpandInTree();
						return FReply::Handled();
					})
			[
				SNew(SImage) // Image overlay for the button
				.Image_Lambda([ItemHandle, this]() {return &(ItemHandle->bExpanded ? GetIcon(FolderOpened) : GetIcon(FolderClosed)); })
				.RenderTransform(FSlateRenderTransform(FScale2D(1.1f)))
			]
			]
		);
		//TableRowBox->SetRenderTransform(FSlateRenderTransform(FScale2D(1.2f)));
		ItemHandle->UpdateFolderIcon();

		FolderImageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
		CloseButtonSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
	}
	CheckBoxSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

	auto Font = FSlateFontInfo(FCoreStyle::GetDefaultFont(), 10);
	if (ItemHandle->Type == Folder) // Slightly larger font for group items
		Font.Size = 12;

	if (ItemHandle->bInRename)
	{
		ItemHandle->RowNameBox->SetContent(
			SNew(SEditableText)
			.Text(FText::FromString(ItemHandle->Name))
			.Font(Font)
			.OnTextChanged_Lambda([ItemHandle](FText Input)
				{
					ItemHandle->Name = Input.ToString();
				})
			.OnTextCommitted_UObject(ItemHandle, &UItemHandle::EndRename));

	}
	else
	{
		ItemHandle->RowNameBox->SetContent(
			SNew(STextBlock)
			.Text(FText::FromString(ItemHandle->Name))
			.Font(Font)
			.ShadowColorAndOpacity(FLinearColor::Blue)
			.ShadowOffset(FIntPoint(-1, 1))
			.OnDoubleClicked_UObject(ItemHandle, &UItemHandle::StartRename));
	}

	if (ItemHandle->bMatchesSearchString)
		ItemHandle->TableRowBox->SetVisibility(EVisibility::Visible);
	else
		ItemHandle->TableRowBox->SetVisibility(EVisibility::Collapsed);
}


void FCradleLightControlEditorModule::GenerateIcons()
{
	FLinearColor OffTint(0.2f, 0.2f, 0.2f, 0.5f);
	FLinearColor UndeterminedTint(0.8f, 0.8f, 0.0f, 0.5f);
	Icons.Emplace(SkyLightOn, *FClassIconFinder::FindThumbnailForClass(ASkyLight::StaticClass()));
	Icons.Emplace(SkyLightOff, Icons[SkyLightOn]);
	Icons[SkyLightOff].TintColor = OffTint;
	Icons.Emplace(SkyLightUndetermined, Icons[SkyLightOn]);
	Icons[SkyLightUndetermined].TintColor = UndeterminedTint;

	Icons.Emplace(DirectionalLightOn, *FClassIconFinder::FindThumbnailForClass(ADirectionalLight::StaticClass()));
	Icons.Emplace(DirectionalLightOff, Icons[DirectionalLightOn]);
	Icons[DirectionalLightOff].TintColor = OffTint;
	Icons.Emplace(DirectionalLightUndetermined, Icons[DirectionalLightOn]);
	Icons[DirectionalLightUndetermined].TintColor = UndeterminedTint;

	Icons.Emplace(SpotLightOn, *FClassIconFinder::FindThumbnailForClass(ASpotLight::StaticClass()));
	Icons.Emplace(SpotLightOff, Icons[SpotLightOn]);
	Icons[SpotLightOff].TintColor = OffTint;
	Icons.Emplace(SpotLightUndetermined, Icons[SpotLightOn]);
	Icons[SpotLightUndetermined].TintColor = UndeterminedTint;

	Icons.Emplace(PointLightOn, *FClassIconFinder::FindThumbnailForClass(APointLight::StaticClass()));
	Icons.Emplace(PointLightOff, Icons[PointLightOn]);
	Icons[PointLightOff].TintColor = OffTint;
	Icons.Emplace(PointLightUndetermined, Icons[PointLightOn]);
	Icons[PointLightUndetermined].TintColor = UndeterminedTint;

	Icons.Emplace(GeneralLightOn, Icons[PointLightOn]);
	Icons.Emplace(GeneralLightOff, Icons[PointLightOff]);
	Icons.Emplace(GeneralLightUndetermined, Icons[PointLightUndetermined]);

	Icons.Emplace(FolderClosed, *FEditorStyle::GetBrush("ContentBrowser.ListViewFolderIcon.Mask"));
	Icons.Emplace(FolderOpened, *FEditorStyle::GetBrush("ContentBrowser.ListViewFolderIcon.Base"));

	for (auto& Icon : Icons)
	{
		//Icon.Value.DrawAs = ESlateBrushDrawType::Box;
		Icon.Value.SetImageSize(FVector2D(24.0f));
	}
}

FCheckBoxStyle FCradleLightControlEditorModule::MakeCheckboxStyleForType(uint8 IconType)
{
	FCheckBoxStyle CheckBoxStyle;
	CheckBoxStyle.CheckedImage = Icons[StaticCast<EIconType>(IconType * 3 + 1)];
	CheckBoxStyle.CheckedHoveredImage = Icons[StaticCast<EIconType>(IconType * 3 + 1)];
	CheckBoxStyle.CheckedPressedImage = Icons[StaticCast<EIconType>(IconType * 3 + 1)];

	CheckBoxStyle.UncheckedImage = Icons[StaticCast<EIconType>(IconType * 3 + 0)];
	CheckBoxStyle.UncheckedHoveredImage = Icons[StaticCast<EIconType>(IconType * 3 + 0)];
	CheckBoxStyle.UncheckedPressedImage = Icons[StaticCast<EIconType>(IconType * 3 + 0)];

	CheckBoxStyle.UndeterminedImage = Icons[StaticCast<EIconType>(IconType * 3 + 2)];
	CheckBoxStyle.UndeterminedHoveredImage = Icons[StaticCast<EIconType>(IconType * 3 + 2)];
	CheckBoxStyle.UndeterminedPressedImage = Icons[StaticCast<EIconType>(IconType * 3 + 2)];

	return CheckBoxStyle;
}

FSlateBrush& FCradleLightControlEditorModule::GetIcon(EIconType Icon)
{
	return Icons[Icon];
}



#undef LOCTEXT_NAMESPACE

IMPLEMENT_MODULE(FCradleLightControlEditorModule, CradleLightControl)