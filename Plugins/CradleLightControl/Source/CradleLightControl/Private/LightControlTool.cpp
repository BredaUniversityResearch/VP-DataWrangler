#include "LightControlTool.h"

#include "Slate.h"

#include "DetailCategoryBuilder.h"
#include "DetailLayoutBuilder.h"
#include "DetailWidgetRow.h"
#include "Chaos/AABB.h"
#include "Engine/Engine.h"
#include "Kismet/GameplayStatics.h"
#include "Engine/World.h"
#include "Editor/EditorEngine.h"
#include "Editor.h"

#include "ClassIconFinder.h"

#include "Engine/SkyLight.h"
#include "Engine/SpotLight.h"
#include "Engine/DirectionalLight.h"
#include "Engine/PointLight.h"

#include "DesktopPlatformModule.h"
#include "IDesktopPlatform.h"

void SLightControlTool::Construct(const FArguments& Args)
{
    if (GWorld)
    {
        auto ActorSpawnedDelegate = FOnActorSpawned::FDelegate::CreateRaw(this, &SLightControlTool::ActorSpawnedCallback);
        ActorSpawnedListenerHandle = GWorld->AddOnActorSpawnedHandler(ActorSpawnedDelegate);
    }
    else
        GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Red, "Could not set actor spawned callback");

    LoadResources();

    ToolTab = Args._ToolTab;
       
    //SHorizontalBox::FSlot* SeparatorSlot;


    GEngine->AddOnScreenDebugMessage(-1, 15.0f, FColor::Emerald, "Light control tool constructed");

    SSplitter::FSlot* SplitterSlot;
    ChildSlot
        [
        SNew(SOverlay)
        + SOverlay::Slot()
        .HAlign(HAlign_Fill)
        .VAlign(VAlign_Top)
        [
            SNew(SSplitter)
            .PhysicalSplitterHandleSize(5.0f)
            .HitDetectionSplitterHandleSize(15.0f)
            +SSplitter::Slot()
            .Expose(SplitterSlot)
            .Value(0.5f)
            //.SizeRule(SSplitter::ESizeRule::SizeToContent)
            [
                SAssignNew(TreeWidget, SLightTreeHierarchy)
                .CoreToolPtr(this)
            ]
            + SSplitter::Slot()
            [
                SNew(SHorizontalBox)
                /*+ SHorizontalBox::Slot()
                .Expose(SeparatorSlot)
                .Padding(0.0f, 0.0f, 30.0f, 0.0f)
                [
                    SNew(SSeparator)
                    .Orientation(EOrientation::Orient_Vertical)
                ]         */
                + SHorizontalBox::Slot()
                [
                    SNew(SVerticalBox)                
                    + LightHeader()
                    + LightPropertyEditor()
                ]
            ]
        ]
    ];

    LightPropertyWidget->FinishInit();

}

SLightControlTool::~SLightControlTool()
{
    //PreDestroy();
}

void SLightControlTool::PreDestroy()
{
    if (TreeWidget)
        TreeWidget->PreDestroy();
    if (LightPropertyWidget)
        LightPropertyWidget->PreDestroy();

    GWorld->RemoveOnActorSpawnedHandler(ActorSpawnedListenerHandle);
}

void SLightControlTool::ActorSpawnedCallback(AActor* Actor)
{
    TreeWidget->OnActorSpawned(Actor);
}

void SLightControlTool::OnTreeSelectionChanged()
{
    if (IsAMasterLightSelected())
    {
        LightPropertyWidget->UpdateSaturationGradient(TreeWidget->SelectionMasterLight->Hue);
        UpdateExtraLightDetailBox();
        UpdateLightHeader();
    }

}

TWeakPtr<SLightTreeHierarchy> SLightControlTool::GetTreeWidget()
{
    return TreeWidget;
}

bool SLightControlTool::OpenFileDialog(FString Title, FString DefaultPath, uint32 Flags, FString FileTypeList, TArray<FString>& OutFilenames)
{
    IDesktopPlatform* Platform = FDesktopPlatformModule::Get();
    return Platform->OpenFileDialog(ToolTab->GetParentWindow()->GetNativeWindow()->GetOSWindowHandle(), Title, DefaultPath, "", FileTypeList, Flags, OutFilenames);
}

bool SLightControlTool::SaveFileDialog(FString Title, FString DefaultPath, uint32 Flags, FString FileTypeList,
    TArray<FString>& OutFilenames)
{
    IDesktopPlatform* Platform = FDesktopPlatformModule::Get();
    return Platform->SaveFileDialog(ToolTab->GetParentWindow()->GetNativeWindow()->GetOSWindowHandle(), Title, DefaultPath, "", FileTypeList, Flags, OutFilenames);
}

FCheckBoxStyle SLightControlTool::MakeCheckboxStyleForType(ETreeItemType IconType)
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

FSlateBrush& SLightControlTool::GetIcon(EIconType Icon)
{
    return Icons[Icon];
}

void SLightControlTool::LoadResources()
{
    GenerateIcons();
}

void SLightControlTool::GenerateIcons()
{
    FLinearColor OffTint(0.2f, 0.2f, 0.2f, 0.5f);
    FLinearColor UndeterminedTint(0.8f, 0.8f, 0.0f, 0.5f);
    FClassIconFinder::FindThumbnailForClass(APointLight::StaticClass());
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
    Icons.Emplace(GeneralLightOff, Icons[PointLightOn]);
    Icons.Emplace(GeneralLightUndetermined, Icons[PointLightUndetermined]);
    
    Icons.Emplace(FolderClosed, *FEditorStyle::GetBrush("ContentBrowser.ListViewFolderIcon.Mask"));
    Icons.Emplace(FolderOpened, *FEditorStyle::GetBrush("ContentBrowser.ListViewFolderIcon.Base"));
    
    for (auto& Icon : Icons)
    {
        //Icon.Value.DrawAs = ESlateBrushDrawType::Box;
        Icon.Value.SetImageSize(FVector2D(24.0f));
    }
}

SVerticalBox::FSlot& SLightControlTool::LightHeader()
{
    auto& Slot = SVerticalBox::Slot();

    Slot.SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    Slot
    .HAlign(HAlign_Fill)
        [
            SAssignNew(LightHeaderBox, SBox)
        ];

    UpdateLightHeader();

    return Slot;
}

void SLightControlTool::UpdateLightHeader()
{
    if (IsAMasterLightSelected() || IsSingleGroupSelected())
    {
        SHorizontalBox::FSlot* NameSlot;
        SHorizontalBox::FSlot* CheckboxSlot;
        SVerticalBox::FSlot* TopSlot;
        TEnumAsByte<ETreeItemType> IconType;
        if (IsSingleGroupSelected())
        {
            IconType = TreeWidget->SelectedItems[0]->Type;
        }
        else
        {
            IconType = TreeWidget->SelectionMasterLight->Type;                        
        }
        for (auto Light : TreeWidget->LightsUnderSelection)
        {
            if (IconType != Light->Type)
            {
                IconType = Mixed;
                break;
            }   
        }
        LightHeaderCheckboxStyle = MakeCheckboxStyleForType(IconType);
        
        LightHeaderBox->SetHAlign(HAlign_Fill);
        LightHeaderBox->SetPadding(FMargin(5.0f, 0.0f));
        LightHeaderBox->SetContent(
            SNew(SVerticalBox)
            +SVerticalBox::Slot()
            .Expose(TopSlot)
            .Padding(0.0f, 0.0f, 0.0f, 5.0f)
            [
                SNew(SHorizontalBox)
                +SHorizontalBox::Slot()
                .HAlign(HAlign_Fill)
                .Expose(NameSlot)
                [
                    SAssignNew(LightHeaderNameBox, SBox)
                   // SNew(STextBlock)
                   // .Text(this, &SLightControlTool::LightHeaderText)
                   // .Font(FSlateFontInfo(FCoreStyle::GetDefaultFont(), 18))
                ]
                
                +SHorizontalBox::Slot()
                .HAlign(HAlign_Right)
                .Padding(0.0f, 0.0f, 15.0f, 0.0f)
                .Expose(CheckboxSlot)
                [
                    SNew(SCheckBox)
                    .Style(&LightHeaderCheckboxStyle)
                    .OnCheckStateChanged(this, &SLightControlTool::OnLightHeaderCheckStateChanged)
                    .IsChecked(this, &SLightControlTool::GetLightHeaderCheckState)
                    .RenderTransform(FSlateRenderTransform(1.2f))
                ]
            ]
            +SVerticalBox::Slot()
            .HAlign(HAlign_Fill)
            .VAlign(VAlign_Bottom)
            .Padding(0.0f, 3.0f, 30.0f, 3.0f)
            [
                SAssignNew(ExtraNoteBox, SBox)                
            ]
            +SVerticalBox::Slot()
            .Padding(0.0f, 5.0f, 0.0f, 0.0f)
            [
                SNew(STextBlock)
                .Text(this, &SLightControlTool::LightHeaderExtraLightsText)
                .Visibility(IsSingleGroupSelected() || AreMultipleLightsSelected() ? 
                    EVisibility::Visible : EVisibility::Collapsed)
            ]
        );
        bItemNoteChangeInProgress = false;
        bItemRenameInProgress = false;
        UpdateItemNameBox();
        UpdateExtraNoteBox();


        //NameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
        CheckboxSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
        TopSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    }
    else
    {
        LightHeaderBox->SetContent(
            SNew(STextBlock)
            .Text(FText::FromString("No lights currently selected"))
            .Font(FSlateFontInfo(FCoreStyle::GetDefaultFont(), 18)));
    }
}

void SLightControlTool::OnLightHeaderCheckStateChanged(ECheckBoxState NewState)
{
    if (IsAMasterLightSelected())
    {
        for (auto Light : TreeWidget->LightsUnderSelection)
        {
            Light->OnCheck(NewState); // Use the callback used by the tree to modify the state
        }
    }
}


ECheckBoxState SLightControlTool::GetLightHeaderCheckState() const
{
    if (IsAMasterLightSelected())
    {
        return TreeWidget->SelectionMasterLight->IsLightEnabled();
    }
    return ECheckBoxState::Undetermined;
}

FText SLightControlTool::LightHeaderExtraLightsText() const
{
    if (TreeWidget->SelectedItems.Num() > 1)
    {
        int GroupCount = 0;
        int LightCount = 0;
        int TotalLightCount = 0;
        for (auto SelectedItem : TreeWidget->SelectedItems)
        {
            if (SelectedItem->Type == Folder)
            {
                GroupCount++;
            }
            else
            {
                LightCount++;
            }
            TotalLightCount += SelectedItem->LightCount();
        }

        return FText::FromString(FString::Printf(TEXT("%d Groups and %d Lights selected (Total %d lights affected)"), GroupCount, LightCount, TotalLightCount));
    }
    return FText::FromString("");
}

void SLightControlTool::UpdateItemNameBox()
{
    auto Item = GetSingleSelectedItem();
    if (Item)
    {
        if (bItemRenameInProgress)
        {
            LightHeaderNameBox->SetContent(
                SNew(SEditableText)
                .Text(this, &SLightControlTool::ItemNameText)
                .OnTextCommitted(this, &SLightControlTool::CommitNewItemName)
                .SelectAllTextWhenFocused(true)                
                .Font(FSlateFontInfo(FCoreStyle::GetDefaultFont(), 18))
            );
        }
        else
        {
            LightHeaderNameBox->SetContent(
                SNew(STextBlock)
                .Text(this, &SLightControlTool::ItemNameText)
                .OnDoubleClicked(this, &SLightControlTool::StartItemNameChange)
                .Font(FSlateFontInfo(FCoreStyle::GetDefaultFont(), 18))
            );
        }
    }
}

FReply SLightControlTool::StartItemNameChange(const FGeometry&, const FPointerEvent&)
{
    bItemRenameInProgress = true;
    UpdateItemNameBox();
    return FReply::Handled();
}

FText SLightControlTool::ItemNameText() const
{
    return FText::FromString(GetSingleSelectedItem()->Name);
}

void SLightControlTool::CommitNewItemName(const FText& Text, ETextCommit::Type CommitType)
{
    if (CommitType == ETextCommit::OnEnter && !Text.IsEmpty())
    {
        auto Item = GetSingleSelectedItem();
        Item->Name = Text.ToString();
        Item->GenerateTableRow();
    }
    bItemRenameInProgress = false;
}

void SLightControlTool::UpdateExtraNoteBox()
{
    ExtraNoteBox->SetVisibility(EVisibility::Visible);
    if (IsSingleGroupSelected())
    {
        ExtraNoteBox->SetContent(
            SNew(STextBlock)
            .Text(FText::FromString("Group")));
    }
    else if (IsAMasterLightSelected())
    {
        auto MasterLight = GetMasterLight();
        if (MasterLight->Note.IsEmpty() && !bItemNoteChangeInProgress)
        {
            ExtraNoteBox->SetContent(
                SNew(STextBlock)
                .Text(FText::FromString("+ Add note"))
                .OnDoubleClicked(this, &SLightControlTool::StartItemNoteChange)
            );
        }
        else if (bItemNoteChangeInProgress)
        {
            ExtraNoteBox->SetContent(
                SNew(SEditableText)
                .Text(this, &SLightControlTool::ItemNoteText)
                .OnTextCommitted(this, &SLightControlTool::CommitNewItemNote)
            );
        }
        else
        {
            ExtraNoteBox->SetContent(
                SNew(STextBlock)
                .Text(this, &SLightControlTool::ItemNoteText)
                .OnDoubleClicked(this, &SLightControlTool::StartItemNoteChange)
            );
        }
    }
    else
        ExtraNoteBox->SetVisibility(EVisibility::Collapsed);
}

FReply SLightControlTool::StartItemNoteChange(const FGeometry&, const FPointerEvent&)
{
    bItemNoteChangeInProgress = true;
    UpdateExtraNoteBox();
    return FReply::Handled();
}

FText SLightControlTool::ItemNoteText() const
{
    auto Item = GetMasterLight();
    if (Item->Note.IsEmpty())
    {
        return FText::FromString("Note");
    }
    return FText::FromString(GetMasterLight()->Note);
}

void SLightControlTool::CommitNewItemNote(const FText& Text, ETextCommit::Type CommitType)
{
    if (CommitType == ETextCommit::OnEnter)
    {
        GetMasterLight()->Note = Text.ToString();
        GetMasterLight()->GenerateTableRow();
    }
    bItemNoteChangeInProgress = false;
    UpdateExtraNoteBox();
}

SVerticalBox::FSlot& SLightControlTool::LightPropertyEditor()
{
    auto& Slot = SVerticalBox::Slot();

    TSharedPtr<SVerticalBox> Box;

    SVerticalBox::FSlot* ExtraLightBoxSlot;

    Slot
        .Padding(20.0f, 30.0f, 20.0f, 0.0f)
        .VAlign(VAlign_Fill)
        .HAlign(HAlign_Fill)
        [
            SNew(SHorizontalBox)
            + SHorizontalBox::Slot() // General light properties + extra light properties or group controls
        [
            SNew(SVerticalBox)
            + SVerticalBox::Slot()
            [
                SAssignNew(LightPropertyWidget, SLightPropertyEditor)
                .CoreToolPtr(this)
            ]
            + SVerticalBox::Slot()
            .Expose(ExtraLightBoxSlot)
            [
                SAssignNew(ExtraLightDetailBox, SBox)
                .Padding(FMargin(0.0f, 5.0f, 0.0f, 0.0f))
            ]
        ]
        + LightSpecificPropertyEditor()
    ];

    ExtraLightBoxSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    UpdateExtraLightDetailBox();

    return Slot;
}

void SLightControlTool::UpdateExtraLightDetailBox()
{
    if (IsAMasterLightSelected())
    {
        if (AreMultipleLightsSelected())
        {
            ExtraLightDetailBox->SetContent(GroupControls());
        }
        else
        {
            ExtraLightDetailBox->SetContent(LightTransformViewer());
        }
    }
    else
        ExtraLightDetailBox->SetContent(SNew(SBox));
}

bool SLightControlTool::IsAMasterLightSelected() const
{
    return GetMasterLight() != nullptr;
}

bool SLightControlTool::IsSingleGroupSelected() const
{
    return TreeWidget && // Tree view exists
        TreeWidget->SelectedItems.Num() == 1 && // There is a single item selected
        TreeWidget->SelectedItems[0]->Type == Folder; // The selected item is a group
}

bool SLightControlTool::AreMultipleLightsSelected() const
{
    return TreeWidget != nullptr && TreeWidget->LightsUnderSelection.Num() > 1;
}

FTreeItem* SLightControlTool::GetMasterLight() const
{
    if (!TreeWidget || !TreeWidget->SelectionMasterLight)
    {
        return nullptr;
    }
    return TreeWidget->SelectionMasterLight.Get();
}

FTreeItem* SLightControlTool::GetSingleSelectedItem() const
{
    if (IsSingleGroupSelected())
        return TreeWidget->SelectedItems[0].Get();
    else
        return GetMasterLight();
}

void SLightControlTool::ClearSelection()
{
    if (TreeWidget)
    {
        TreeWidget->SelectedItems.Empty();
        TreeWidget->Tree->ClearSelection();
        TreeWidget->SelectionMasterLight = nullptr;
        TreeWidget->LightsUnderSelection.Empty();
    }
    UpdateLightHeader();
    UpdateExtraLightDetailBox();
}


TSharedRef<SBox> SLightControlTool::LightTransformViewer()
{
    SHorizontalBox::FSlot* ButtonsSlot;

    TSharedPtr<SBox> Box;

    SAssignNew(Box, SBox)
    [
        SNew(SBorder)
        .HAlign(HAlign_Fill)
        .VAlign(VAlign_Fill)
        [
            SNew(SHorizontalBox)
            +SHorizontalBox::Slot()
            .HAlign(HAlign_Fill)
            [
                SNew(SVerticalBox)
                +SVerticalBox::Slot()
                .VAlign(VAlign_Center)
                .HAlign(HAlign_Fill)
                .Padding(5.0f, 3.0f)
                [
                    SNew(SHorizontalBox)
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Fill)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("Parent Object"))
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Fill)
                    [
                        SNew(STextBlock)
                        .Text(this, &SLightControlTool::GetItemParentName)
                    ]
                ]
                +SVerticalBox::Slot()
                .VAlign(VAlign_Center)
                .HAlign(HAlign_Fill)
                .Padding(5.0f, 3.0f)
                [
                    SNew(SHorizontalBox)
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Fill)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("Position"))
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Fill)
                    [
                        SNew(STextBlock)
                        .Text(this, &SLightControlTool::GetItemPosition)
                    ]
                ]
                +SVerticalBox::Slot()
                .VAlign(VAlign_Center)
                .HAlign(HAlign_Fill)
                .Padding(5.0f, 3.0f)
                [
                    SNew(SHorizontalBox)
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Fill)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("Rotation"))
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Fill)
                    [
                        SNew(STextBlock)
                        .Text(this, &SLightControlTool::GetItemRotation)
                    ]
                ]
                +SVerticalBox::Slot()
                .VAlign(VAlign_Center)
                .HAlign(HAlign_Fill)
                .Padding(5.0f, 3.0f)
                [
                    SNew(SHorizontalBox)
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Fill)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("Scale"))
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Fill)
                    [
                        SNew(STextBlock)
                        .Text(this, &SLightControlTool::GetItemScale)
                    ]
                ]
            ]
            +SHorizontalBox::Slot()
            .Expose(ButtonsSlot)
            [
                SNew(SVerticalBox)
                +SVerticalBox::Slot()
                .Padding(5.0f)
                [
                    SNew(SButton)
                    .Text(FText::FromString("Select Scene Object"))
                    .OnClicked(this, &SLightControlTool::SelectItemInScene)
                ]
                + SVerticalBox::Slot()
                .Padding(5.0f)
                [
                    SNew(SButton)
                    .Text(FText::FromString("Select Parent Object"))
                    .IsEnabled(this, &SLightControlTool::SelectItemParentButtonEnable)
                    .OnClicked(this, &SLightControlTool::SelectItemParent)
                ]
            ]
        ]
    ];

    ButtonsSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    return Box.ToSharedRef();
}

FReply SLightControlTool::SelectItemInScene()
{
    if (IsAMasterLightSelected())
    {
        GEditor->SelectNone(true, true);
        GEditor->SelectActor(TreeWidget->SelectionMasterLight->ActorPtr, true, true, false, true);        
    }

    return FReply::Handled();
}

FReply SLightControlTool::SelectItemParent()
{
    GEditor->SelectNone(true, true);
    GEditor->SelectActor(TreeWidget->SelectionMasterLight->ActorPtr->GetAttachParentActor(), true, true, false, true);

    return FReply::Handled();
}

bool SLightControlTool::SelectItemParentButtonEnable() const
{
    return IsAMasterLightSelected() && TreeWidget->SelectionMasterLight->ActorPtr->GetAttachParentActor();
}

FText SLightControlTool::GetItemParentName() const
{
    if (IsAMasterLightSelected() && TreeWidget->SelectionMasterLight->ActorPtr->GetAttachParentActor())
    {
        return FText::FromString(TreeWidget->SelectionMasterLight->ActorPtr->GetAttachParentActor()->GetName());
    }
    return FText::FromString("None");
}

FText SLightControlTool::GetItemPosition() const
{
    if (IsAMasterLightSelected())
    {
        return FText::FromString(TreeWidget->SelectionMasterLight->ActorPtr->GetActorLocation().ToString());
    }
    return FText::FromString("");
}

FText SLightControlTool::GetItemRotation() const
{
    if (IsAMasterLightSelected())
    {
        return FText::FromString(TreeWidget->SelectionMasterLight->ActorPtr->GetActorRotation().ToString());
    }
    return FText::FromString("");
}

FText SLightControlTool::GetItemScale() const
{
    if (IsAMasterLightSelected())
    {
        return FText::FromString(TreeWidget->SelectionMasterLight->ActorPtr->GetActorScale().ToString());
    }
    return FText::FromString("");
}

TSharedRef<SBox> SLightControlTool::GroupControls()
{
    TSharedPtr<SBox> Box;

    
    SVerticalBox::FSlot* MasterLightSlot;
    SAssignNew(Box, SBox)
    [
        SNew(SBorder)
        .Padding(FMargin(5.0f, 5.0f))
        [
            SNew(SVerticalBox)
            + SVerticalBox::Slot()
            .Expose(MasterLightSlot)
            [
                SNew(SHorizontalBox)
                + SHorizontalBox::Slot()
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("Master Light"))
                ]
                + SHorizontalBox::Slot()
                [
                    SNew(SComboBox<TSharedPtr<FTreeItem>>)
                    .OptionsSource(&TreeWidget->LightsUnderSelection)
                    .OnGenerateWidget(this, &SLightControlTool::GroupControlDropDownLabel)
                    .OnSelectionChanged(this, &SLightControlTool::GroupControlDropDownSelection)
                    .InitiallySelectedItem(TreeWidget->SelectionMasterLight)[
                        SNew(STextBlock).Text(this, &SLightControlTool::GroupControlDropDownDefaultLabel)
                    ]
                ]
            ]
            + SVerticalBox::Slot()
            [
                SNew(SHorizontalBox)
                + SHorizontalBox::Slot()
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("Affected lights"))
                ]
                + SHorizontalBox::Slot()
                [
                    SNew(STextBlock)
                    .Text(this, &SLightControlTool::GroupControlLightList)
                    .AutoWrapText(true)
                ]
            ]
        ]
    ];

    MasterLightSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;


    return Box.ToSharedRef();
}

TSharedRef<SWidget> SLightControlTool::GroupControlDropDownLabel(TSharedPtr<FTreeItem> Item)
{
    if (Item->Type == Folder)
    {
        return SNew(SBox);
    }
    return SNew(STextBlock).Text(FText::FromString(Item->Name));
}

void SLightControlTool::GroupControlDropDownSelection(TSharedPtr<FTreeItem> Item, ESelectInfo::Type SelectInfoType)
{
    TreeWidget->SelectionMasterLight = Item;
}

FText SLightControlTool::GroupControlDropDownDefaultLabel() const
{
    if (TreeWidget->SelectionMasterLight)
    {
        return FText::FromString(TreeWidget->SelectionMasterLight->Name);
    }
    return FText::FromString("");
}

FText SLightControlTool::GroupControlLightList() const
{
    FString LightList = TreeWidget->LightsUnderSelection[0]->Name;

    for (size_t i = 1; i < TreeWidget->LightsUnderSelection.Num(); i++)
    {
        LightList += ", ";
        LightList += TreeWidget->LightsUnderSelection[i]->Name;
    }

    return FText::FromString(LightList);
}

SHorizontalBox::FSlot& SLightControlTool::LightSpecificPropertyEditor()
{
    auto& Slot = SHorizontalBox::Slot();
    Slot.SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    SVerticalBox::FSlot* HorizontalNameSlot, * HorizontalDegreesSlot, * HorizontalPercentageSlot;
    SVerticalBox::FSlot* VerticalNameSlot, * VerticalDegreesSlot, * VerticalPercentageSlot;
    SVerticalBox::FSlot* AngleNameSlot, *AngleDegreesSlot, *AnglePercentageSlot;

    Slot
    .Padding(5.0f, 0.0f, 0.0f, 0.0f)
    [
        SNew(SBorder)
        .HAlign(HAlign_Fill)
        .VAlign(VAlign_Fill)
        [
            SNew(SVerticalBox)
            +SVerticalBox::Slot()
            [
                SNew(SHorizontalBox)
                +SHorizontalBox::Slot()
                .Padding(5.0f)
                [
                    SNew(SVerticalBox)
                    +SVerticalBox::Slot()
                    .Expose(HorizontalNameSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("Horizontal"))
                    ]
                    +SVerticalBox::Slot()
                    .Expose(HorizontalDegreesSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("0"))
                    ]
                    +SVerticalBox::Slot()
                    [
                        SNew(SSlider)
                        .Orientation(Orient_Vertical)
                    ]
                    + SVerticalBox::Slot()
                    .Expose(HorizontalPercentageSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("50%"))
                    ]
                ]
                +SHorizontalBox::Slot()
                .Padding(5.0f)
                [
                    SNew(SVerticalBox)
                    +SVerticalBox::Slot()
                    .Expose(VerticalNameSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("Vertical"))
                    ]
                    +SVerticalBox::Slot()
                    .Expose(VerticalDegreesSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("0"))
                    ]
                    +SVerticalBox::Slot()
                    [
                        SNew(SSlider)
                        .Orientation(Orient_Vertical)
                    ]
                    + SVerticalBox::Slot()
                    .Expose(VerticalPercentageSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("50%"))
                    ]
                ]
                +SHorizontalBox::Slot()
                .Padding(5.0f)
                [
                    SNew(SVerticalBox)
                    +SVerticalBox::Slot()
                    .Expose(AngleNameSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("Angle"))
                    ]
                    +SVerticalBox::Slot()
                    .Expose(AngleDegreesSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("0"))
                    ]
                    +SVerticalBox::Slot()
                    [
                        SNew(SSlider)
                        .Orientation(Orient_Vertical)
                    ]
                    + SVerticalBox::Slot()
                    .Expose(AnglePercentageSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("50%"))
                    ]
                ]
            ]
        ]
    ];

    HorizontalNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    HorizontalDegreesSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    HorizontalPercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    VerticalNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    VerticalDegreesSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    VerticalPercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    AngleNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    AngleDegreesSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    AnglePercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;



    return Slot;
}

