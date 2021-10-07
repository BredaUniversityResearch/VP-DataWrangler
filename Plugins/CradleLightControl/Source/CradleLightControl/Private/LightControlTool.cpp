#include "LightControlTool.h"

#include "Slate.h"

#include "DetailCategoryBuilder.h"
#include "DetailLayoutBuilder.h"
#include "DetailWidgetRow.h"
#include "Chaos/AABB.h"
#include "Components/LightComponent.h"
#include "Components/SkyLightComponent.h"
#include "Engine/Engine.h"
#include "Interfaces/IPluginManager.h"
#include "Kismet/GameplayStatics.h"
#include "Engine/World.h"
#include "Editor/EditorEngine.h"
#include "Editor.h"

#include "IDesktopPlatform.h"
#include "DesktopPlatformModule.h"


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

FText SLightControlTool::TestTextGetter() const
{
    FString N = "Nothing Selected";

    if (IsLightSelected())
    {
        N = TreeWidget->SelectionMasterLight->Name;
    }

    return FText::FromString(N);
}

void SLightControlTool::ActorSpawnedCallback(AActor* Actor)
{
    TreeWidget->OnActorSpawned(Actor);
}

void SLightControlTool::OnTreeSelectionChanged()
{
    if (IsLightSelected())
    {
        LightPropertyWidget->UpdateSaturationGradient(TreeWidget->SelectionMasterLight->Hue);
        UpdateExtraLightDetailBox();
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

void SLightControlTool::LoadResources()
{

}

SVerticalBox::FSlot& SLightControlTool::LightHeader()
{
    auto& Slot = SVerticalBox::Slot();

    Slot.SizeParam.SizeRule = FSizeParam::SizeRule_Auto;


    Slot
    .HAlign(HAlign_Fill)
        [
            SNew(SHorizontalBox)
            +SHorizontalBox::Slot()
            .HAlign(HAlign_Left)
            [
                SNew(STextBlock)
                .Font(FSlateFontInfo(FCoreStyle::GetDefaultFont(), 18))
                .Text(this, &SLightControlTool::TestTextGetter)
            ]
            +SHorizontalBox::Slot()
            .HAlign(HAlign_Right)
            [
                SNew(SCheckBox)
                .IsEnabled_Lambda([this]() {return TreeWidget->SelectionMasterLight != nullptr; })
            ]

        ];

    return Slot;
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
    if (IsLightSelected())
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

bool SLightControlTool::IsLightSelected() const
{
    return TreeWidget != nullptr && TreeWidget->SelectionMasterLight != nullptr;
}

bool SLightControlTool::AreMultipleLightsSelected() const
{
    return TreeWidget != nullptr && TreeWidget->LightsUnderSelection.Num() > 1;
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
    if (IsLightSelected())
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
    return IsLightSelected() && TreeWidget->SelectionMasterLight->ActorPtr->GetAttachParentActor();
}

FText SLightControlTool::GetItemParentName() const
{
    if (IsLightSelected() && TreeWidget->SelectionMasterLight->ActorPtr->GetAttachParentActor())
    {
        return FText::FromString(TreeWidget->SelectionMasterLight->ActorPtr->GetAttachParentActor()->GetName());
    }
    return FText::FromString("None");
}

FText SLightControlTool::GetItemPosition() const
{
    if (IsLightSelected())
    {
        return FText::FromString(TreeWidget->SelectionMasterLight->ActorPtr->GetActorLocation().ToString());
    }
    return FText::FromString("");
}

FText SLightControlTool::GetItemRotation() const
{
    if (IsLightSelected())
    {
        return FText::FromString(TreeWidget->SelectionMasterLight->ActorPtr->GetActorRotation().ToString());
    }
    return FText::FromString("");
}

FText SLightControlTool::GetItemScale() const
{
    if (IsLightSelected())
    {
        return FText::FromString(TreeWidget->SelectionMasterLight->ActorPtr->GetActorScale().ToString());
    }
    return FText::FromString("");
}

TSharedRef<SBox> SLightControlTool::GroupControls()
{
    TSharedPtr<SBox> Box;

    

    SAssignNew(Box, SBox)
    [
        SNew(SBorder)
        .Padding(FMargin(5.0f, 5.0f))
        [
            SNew(SVerticalBox)
            + SVerticalBox::Slot()
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

