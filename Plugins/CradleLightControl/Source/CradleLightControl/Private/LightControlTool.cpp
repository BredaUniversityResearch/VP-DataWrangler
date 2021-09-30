#include "LightControlTool.h"

#include "Slate.h"

#include "DetailCategoryBuilder.h"
#include "DetailLayoutBuilder.h"
#include "DetailWidgetRow.h"
#include "Chaos/AABB.h"
#include "Chaos/AABB.h"
#include "Chaos/AABB.h"
#include "Chaos/AABB.h"
#include "Components/LightComponent.h"
#include "Components/SkyLightComponent.h"
#include "Engine/Engine.h"
#include "Interfaces/IPluginManager.h"
#include "Kismet/GameplayStatics.h"
#include "Engine/World.h"

#include "Engine/SkyLight.h"
#include "Engine/PointLight.h"
#include "Engine/SpotLight.h"
#include "Engine/DirectionalLight.h"


FTreeItem::FTreeItem(SLightControlTool* InOwningWidget, FString InName, TArray<TSharedPtr<FTreeItem>> InChildren)
    : Name(InName)
    , Children(InChildren)
    , OwningWidget(InOwningWidget)
    , bInRename(false)
{
}

ECheckBoxState FTreeItem::IsLightEnabled() const
{
    bool AllOff = true, AllOn = true;

    switch (Type)
    {
    case ETreeItemType::SkyLight:
        return SkyLight->GetLightComponent()->IsVisible() ? ECheckBoxState::Checked : ECheckBoxState::Unchecked;
    case ETreeItemType::SpotLight:
        return SpotLight->GetLightComponent()->IsVisible() ? ECheckBoxState::Checked : ECheckBoxState::Unchecked;
    case ETreeItemType::DirectionalLight:
        return DirectionalLight->GetLightComponent()->IsVisible() ? ECheckBoxState::Checked : ECheckBoxState::Unchecked;
    case ETreeItemType::PointLight:
        return PointLight->GetLightComponent()->IsVisible() ? ECheckBoxState::Checked : ECheckBoxState::Unchecked;
    case ETreeItemType::Folder:
        for (auto& Child : Children)
        {
            auto State = Child->IsLightEnabled();
            if (State == ECheckBoxState::Checked)
                AllOff = false;
            else if (State == ECheckBoxState::Unchecked)
                AllOn = false;
            else if (State == ECheckBoxState::Undetermined)
                return ECheckBoxState::Undetermined;

            if (!AllOff && !AllOn)
                return ECheckBoxState::Undetermined;
        }

        if (AllOn)
            return ECheckBoxState::Checked;
        else
            return ECheckBoxState::Unchecked;


    default:
        return ECheckBoxState::Undetermined;
    }
}

void FTreeItem::OnCheck(ECheckBoxState NewState)
{
    bool B = false;
    if (NewState == ECheckBoxState::Checked)
        B = true;

    switch (Type)
    {
    case ETreeItemType::SkyLight:
        SkyLight->GetLightComponent()->SetVisibility(B);
        break;
    case ETreeItemType::SpotLight:
        SpotLight->GetLightComponent()->SetVisibility(B);
        break;
    case ETreeItemType::DirectionalLight:
        DirectionalLight->GetLightComponent()->SetVisibility(B);
        break;
    case ETreeItemType::PointLight:
        PointLight->GetLightComponent()->SetVisibility(B);
        break;
    case ETreeItemType::Folder:
        for (auto& Child : Children)
        {
            Child->OnCheck(NewState);
        }
        break;
    }
}

void FTreeItem::GenerateTableRow()
{
    SHorizontalBox::FSlot& CheckBoxSlot = SHorizontalBox::Slot();
    CheckBoxSlot.SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    if (!Type == Folder || Children.Num() > 0)
    {
        CheckBoxSlot[
            SNew(SCheckBox)
                .IsChecked_Raw(this, &FTreeItem::IsLightEnabled)
                .OnCheckStateChanged_Raw(this, &FTreeItem::OnCheck)
        ];
    }
    else
        GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Black, FString::Printf(TEXT("%s %d"), *Name, Children.Num()));


    TableRowBox->SetContent(
        SNew(SHorizontalBox)
        + SHorizontalBox::Slot()
        [
            SAssignNew(TextSlot, SBox)    
        ]
        + CheckBoxSlot);

    if (bInRename)
    {
        TextSlot->SetContent(
            SNew(SEditableText)
            .Text(FText::FromString(Name))
            .OnTextChanged_Lambda([this](FText Input)
                {
                    Name = Input.ToString();
                })
            /*.OnKeyDownHandler(FOnKeyDown::CreateLambda([this](const FGeometry&, const FKeyEvent& KeyEvent)
                {
                    GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Black, KeyEvent.GetKey().ToString());

                    if (KeyEvent.GetKey().ToString() == "Enter")
                    {
                        EndRename();
                    }

                    return FReply::Handled();
                }))*/
                    .OnTextCommitted(this, &FTreeItem::EndRename));
        
    }
    else
    {
        TextSlot->SetContent(
            SNew(STextBlock)
            .Text(FText::FromString(Name))
            .ShadowColorAndOpacity(FLinearColor::Blue)
            .ShadowOffset(FIntPoint(-1, 1))
            .OnDoubleClicked(this, &FTreeItem::StartRename));
    }
}

bool FTreeItem::VerifyDragDrop(TSharedPtr<FTreeItem> Dragged, TSharedPtr<FTreeItem> Destination)
{
    // Would result in the child and parent creating a circle
    if (Dragged->Children.Find(Destination) != INDEX_NONE)
    {
        GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Green, "Cannot drag parent to child");
        return false;
    }

    // Can't drag the item on itself, can we now
    if (Dragged == Destination)
    {
        return false;
    }


    // Would cause a circular dependency between the tree items
    if (Dragged->HasAsIndirectChild(Destination))
    {
        GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Green, "Cannot drag parent to indirect child");
        return false;
    }

    // No need to do anything in this case
    if (Destination->Children.Find(Dragged) != INDEX_NONE)
    {
        GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Green, "Drag drop cancelled");
        return false;
    }

    return true;
}

bool FTreeItem::HasAsIndirectChild(TSharedPtr<FTreeItem> Item)
{
    if (Children.Find(Item) != INDEX_NONE)
        return true;

    for (auto TreeItem : Children)
    {
        if (TreeItem->HasAsIndirectChild(Item))
            return true;
    }

    return false;
}

FReply FTreeItem::StartRename(const FGeometry&, const FPointerEvent&)
{
    bInRename = true;
    GenerateTableRow();
    return FReply::Handled();
}


void FTreeItem::EndRename(const FText& Text, ETextCommit::Type CommitType)
{
    if (ETextCommit::Type::OnEnter == CommitType)
    {
        Name = Text.ToString();
    }


    bInRename = false;
    GenerateTableRow();
}

FReply FTreeItem::TreeDragDetected(const FGeometry& Geometry, const FPointerEvent& MouseEvent)
{
    GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Green, Name + "Dragggg");


    TSharedRef<FTreeDropOperation> DragDropOp = MakeShared<FTreeDropOperation>();
    DragDropOp->DraggedItem = SharedThis(this);

    FReply Reply = FReply::Handled();

    Reply.BeginDragDrop(DragDropOp);

    return Reply;
}

FReply FTreeItem::TreeDropDetected(const FDragDropEvent& DragDropEvent)
{
    auto DragDrop = StaticCastSharedPtr<FTreeDropOperation>(DragDropEvent.GetOperation());
    auto Target = DragDrop->DraggedItem;
    auto Source = Target->Parent;

    if (!VerifyDragDrop(Target, SharedThis(this)))
    {
        GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Green, "Drag drop cancelled");
        auto Reply = FReply::Handled();
        Reply.EndDragDrop();

        return FReply::Handled();
    }

    if (Type == Folder)
    {
        if (true)
        {
            
        }

        auto Destination = SharedThis(this);

        if (Source)
            Source->Children.Remove(Target);
        else
            OwningWidget->TreeItems.Remove(Target);
        Destination->Children.Add(Target);
        Target->Parent = Destination;

        if (Source)
            Source->GenerateTableRow();
        Destination->GenerateTableRow();        
    }
    else
    {

        auto Destination = MakeShared<FTreeItem>();
        Destination->Name = Name + " Group";
        Destination->OwningWidget = OwningWidget;
        Destination->Type = Folder;
        Destination->Parent = Parent;

        if (Parent)
        {
            Parent->Children.Remove(SharedThis(this));
            Parent->Children.Add(Destination);
        }
        else
        {
            OwningWidget->TreeItems.Remove(SharedThis(this));
            OwningWidget->TreeItems.Add(Destination);
        }

        if (Source)
            Source->Children.Remove(Target);
        else
            OwningWidget->TreeItems.Remove(Target);

        Destination->Children.Add(Target);
        Destination->Children.Add(SharedThis(this));

        auto PrevParent = Parent;

        Target->Parent = Destination;
        Parent = Destination;

        if (PrevParent)
            PrevParent->GenerateTableRow();
        if (Source)
            Source->GenerateTableRow();
    }

    OwningWidget->Tree->RequestTreeRefresh();

    auto Reply = FReply::Handled();
    Reply.EndDragDrop();

    return FReply::Handled();
}

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
    // Create a test data set
    // TODO: Replace with data from the editor 
    /*TreeItems = {
        MakeShared<FTreeItem>(this, "Root 1"),
        MakeShared<FTreeItem>(this, "Root 2"),
        MakeShared<FTreeItem>(this, "Root 3"),
        MakeShared<FTreeItem>(this, "Root 4"),
    };

    TreeItems[0]->Children.Add(MakeShared<FTreeItem>(this, "Child 1"));
    TreeItems[0]->Children.Add(MakeShared<FTreeItem>(this, "Child 2"));

    TreeItems[0]->Children[0]->Children.Add(MakeShared<FTreeItem>(this, "Child-child 1"));

    TreeItems[0]->Children[0]->Parent = TreeItems[0];
    TreeItems[0]->Children[1]->Parent = TreeItems[0];

    TreeItems[0]->Children[0]->Children[0]->Parent = TreeItems[0]->Children[0];*/
       
    UpdateLightList();

    // SVerticalBox slots are by default dividing the space equally between each other
    // Because of this we need to expose the slot with the search bar in order to disable that for it
    SVerticalBox::FSlot* LightSearchBarSlot;
    SVerticalBox::FSlot* NewFolderButtonSlot;
    //SHorizontalBox::FSlot* SeparatorSlot;
    FSlateFontInfo Font24(FCoreStyle::GetDefaultFont(), 20);


    GEngine->AddOnScreenDebugMessage(-1, 15.0f, FColor::Emerald, "Light control tool constructed");

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
            [
                SNew(SVerticalBox) // Light selection menu thingy
                +SVerticalBox::Slot()
                .Expose(LightSearchBarSlot)
                .HAlign(HAlign_Fill)
                .VAlign(VAlign_Top)
                [
                    SNew(SVerticalBox)
                    +SVerticalBox::Slot()
                    .VAlign(VAlign_Top)
                    [
                        SNew(SSearchBox) // Search bar for light
                    ]
                    +SVerticalBox::Slot()
                    .VAlign(VAlign_Top)
                    .AutoHeight()
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("Scene Lights"))
                        .Font(Font24)
                    ]
                ]
                +SVerticalBox::Slot()
                .Padding(0.0f, 0.0f, 8.0f, 0.0f)
                .HAlign(HAlign_Fill)
                .VAlign(VAlign_Top)
                [
                    SNew(SBox)
                    .VAlign(VAlign_Top)
                    .HAlign(HAlign_Fill)
                    [
                        SAssignNew(Tree, STreeView<TSharedPtr<FTreeItem>>)
                        .ItemHeight(24.0f)
                        .TreeItemsSource(&TreeItems)
                        .OnSelectionChanged(this, &SLightControlTool::SelectionCallback)
                        .OnGenerateRow(this, &SLightControlTool::AddToTree)
                        .OnGetChildren(this, &SLightControlTool::GetChildren)
                    ]
                ]
                +SVerticalBox::Slot()
                .VAlign(VAlign_Bottom)
                .Padding(0.0f, 10.0f, 0.0f, 0.0f)
                .Expose(NewFolderButtonSlot)
                [
                    SNew(SButton)
                    .Text(FText::FromString("Add New Group"))
                    .OnClicked_Raw(this, &SLightControlTool::AddFolderToTree)
                ]
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
    LightSearchBarSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    NewFolderButtonSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    //SeparatorSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    for (auto& TreeItem : TreeItems)
    {
        Tree->SetItemExpansion(TreeItem, true);
    }
}

SLightControlTool::~SLightControlTool()
{
    PreDestroy();
}

void SLightControlTool::PreDestroy()
{
    //if (IntensityGradientTexture)
    {
        IntensityGradientTexture->ConditionalBeginDestroy();
        IntensityGradientTexture->RemoveFromRoot();
        HSVGradientTexture->ConditionalBeginDestroy();
        HSVGradientTexture->RemoveFromRoot();
        SaturationGradientTexture->ConditionalBeginDestroy();
        SaturationGradientTexture->RemoveFromRoot();
        TemperatureGradientTexture->ConditionalBeginDestroy();
        TemperatureGradientTexture->RemoveFromRoot();
        //IntensityGradientTexture.Reset();        
    }

}


TSharedRef<ITableRow> SLightControlTool::AddToTree(TSharedPtr<FTreeItem> Item, const TSharedRef<STableViewBase>& OwnerTable)
{
    SHorizontalBox::FSlot& CheckBoxSlot = SHorizontalBox::Slot();
    CheckBoxSlot.SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    if (!Item->Type == Folder || Item->Children.Num() > 0)
    {
        CheckBoxSlot[
            SNew(SCheckBox)
                .IsChecked_Raw(Item.Get(), &FTreeItem::IsLightEnabled)
                .OnCheckStateChanged_Raw(Item.Get(), &FTreeItem::OnCheck)
        ];
    }
    else
        GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Black, FString::Printf(TEXT("%s %d"), *Item->Name, Item->Children.Num()));

    auto Row = 
        SNew(STableRow<TSharedPtr<FString>>, OwnerTable)
        .Padding(2.0f)
        .OnDragDetected(Item.Get(), &FTreeItem::TreeDragDetected)
        .OnDrop_Raw(Item.Get(), &FTreeItem::TreeDropDetected)
        [
            SAssignNew(Item->TableRowBox, SBox)           
        ];

    Item->GenerateTableRow();

    return Row;
        


}

void SLightControlTool::GetChildren(TSharedPtr<FTreeItem> Item, TArray<TSharedPtr<FTreeItem>>& Children)
{
    Children.Append(Item->Children);
}

void SLightControlTool::SelectionCallback(TSharedPtr<FTreeItem> Item, ESelectInfo::Type SelectType)
{
    SelectedItems = Tree->GetSelectedItems();
}

FReply SLightControlTool::AddFolderToTree()
{
    TSharedPtr<FTreeItem> NewFolder = MakeShared<FTreeItem>();
    NewFolder->OwningWidget = this;
    NewFolder->Type = Folder;
    NewFolder->Name = "New Group";
    NewFolder->Parent = nullptr;

    TreeItems.Add(NewFolder);

    Tree->RequestTreeRefresh();

    return FReply::Handled();
}

FText SLightControlTool::TestTextGetter() const
{

    FString N = "Nothing Selected";

    if (SelectedItems.Num())
    {
        N = SelectedItems[0]->Name;
    }

    return FText::FromString(N);
}

void SLightControlTool::ActorSpawnedCallback(AActor* Actor)
{
    auto Type = Invalid;

    if (Cast<ASkyLight>(Actor))
        Type = ETreeItemType::SkyLight;
    else if (Cast<ASpotLight>(Actor))
        Type = ETreeItemType::SpotLight;
    else if (Cast<ADirectionalLight>(Actor))
        Type = ETreeItemType::DirectionalLight;
    else if (Cast<APointLight>(Actor))
        Type = ETreeItemType::PointLight;

    if (Type != Invalid)
    {
        auto NewItem = MakeShared<FTreeItem>();

        NewItem->Parent = nullptr;
        NewItem->OwningWidget = this;
        NewItem->Name = Actor->GetName();
        NewItem->Type = Type;

        switch (Type)
        {
        case SkyLight:
            NewItem->SkyLight = Cast<ASkyLight>(Actor);
            break;
        case SpotLight:
            NewItem->SpotLight = Cast<ASpotLight>(Actor);
            break;
        case DirectionalLight:
            NewItem->DirectionalLight = Cast<ADirectionalLight>(Actor);
            break;
        case PointLight:
            NewItem->PointLight = Cast<APointLight>(Actor);
            break;
        }

        TreeItems.Add(NewItem);

        Tree->RequestTreeRefresh();
    }
}

TArray<FColor> SLightControlTool::LinearGradient(TArray<FColor> ControlPoints, FVector2D Size, EOrientation Orientation)
{
    TArray<FColor> GradientPixels;
    if (ControlPoints.Num())
    {
        if (ControlPoints.Num() == 1)
        {
            for (size_t x = 0; x < Size.X; x++)
            {
                for (size_t y = 0; y < Size.Y; y++)
                {
                    GradientPixels.Add(ControlPoints[0]);
                }
            }
        }
        else
        {
            auto NumSteps = ControlPoints.Num() - 1;
            int TotalStepSize;
            int RepeatCount;
            if (Orientation == Orient_Vertical)
            {
                TotalStepSize = Size.Y;
                RepeatCount = Size.X;
            }
            else
            {
                TotalStepSize = Size.X;
                RepeatCount = Size.Y;
            }
            auto StepSize = TotalStepSize / NumSteps;

            for (auto Rep = 0; Rep < RepeatCount; Rep++)
            {
                for (auto Pixel = 0; Pixel < TotalStepSize; Pixel++)
                {
                    auto Progress = StaticCast<float>(Pixel) / StaticCast<float>(StepSize);
                    auto BeforeId = StaticCast<int>(FMath::Floor(Progress)); // Avoid setting Bef
                    auto Alpha = FMath::Frac(Progress);
                    FLinearColor Before, After;
                    Before = ControlPoints[BeforeId];
                    After = ControlPoints[FMath::Min(ControlPoints.Num() - 1, BeforeId + 1)];

                    auto lc = ((1.0f - Alpha) * Before + Alpha * After);
                    FColor c(lc.ToFColor(false));
                    GradientPixels.Add(c);
                    
                }
            }

        }
    }

    return GradientPixels;
}

TArray<FColor> SLightControlTool::HSVGradient(FVector2D Size, EOrientation Orientation)
{
    int TotalStepSize;
    int RepeatCount;
    if (Orientation == Orient_Vertical)
    {
        TotalStepSize = Size.Y;
        RepeatCount = Size.X;
    }
    else
    {
        TotalStepSize = Size.X;
        RepeatCount = Size.Y;
    }
    TArray<FColor> GradientPixels;

    for (auto Rep = 0; Rep < RepeatCount; Rep++)
    {
        for (auto Pixel = 0; Pixel < TotalStepSize; Pixel++)
        {
            auto H = StaticCast<uint8>(StaticCast<float>(Pixel) / StaticCast<float>(TotalStepSize) * 255);

            GradientPixels.Add(FLinearColor::MakeFromHSV8(H, 255, 255).ToFColor(false));
        }
    }

    return GradientPixels;
}

UTexture2D* SLightControlTool::MakeGradientTexture(int X, int Y)
{
    auto Tex = UTexture2D::CreateTransient(X, Y);
    Tex->CompressionSettings = TextureCompressionSettings::TC_VectorDisplacementmap;
    Tex->SRGB = 0;
    Tex->AddToRoot();
    Tex->UpdateResource();
    return Tex;
}

void SLightControlTool::LoadResources()
{

    
    GenerateTextures();


    


    const FVector2D GradientSize(20.0f, 256.0f);

    IntensityGradientBrush = MakeShared<FSlateImageBrush>(IntensityGradientTexture, GradientSize);
    HSVGradientBrush = MakeShared<FSlateImageBrush>(HSVGradientTexture, GradientSize);
    SaturationGradientBrush = MakeShared<FSlateImageBrush>(SaturationGradientTexture, GradientSize);
    TemperatureGradientBrush = MakeShared<FSlateImageBrush>(TemperatureGradientTexture, GradientSize);

}

void SLightControlTool::GenerateTextures()
{

    IntensityGradientTexture = MakeGradientTexture();
    HSVGradientTexture = MakeGradientTexture();
    SaturationGradientTexture = MakeGradientTexture();
    TemperatureGradientTexture = MakeGradientTexture();

    auto l = [this](FRHICommandListImmediate& RHICmdList)
    {
        auto UpdateRegion = FUpdateTextureRegion2D(0, 0, 0, 0, 1, 256);

    
        TArray<FColor> IntensityGradientPixels = LinearGradient(TArray<FColor>{
            FColor::Black,
            FColor::White
        });

        TArray<FColor> HSVGradientPixels = LinearGradient(TArray<FColor>{
            FColor::Red,
            FColor::Yellow,
            FColor::Green,
            FColor::Cyan,
            FColor::Blue,
            FColor::Magenta,
            FColor::Red
        });

        TArray<FColor> SaturationGradientPixels = LinearGradient(TArray<FColor>{
            FColor::White,
            FColor::Red
        });

        TArray<FColor> TemperatureGradientPixels = LinearGradient(TArray<FColor>{
            FColor::Cyan,
            FColor::White,
            FColor::Yellow,
            FColor::Red
        });




        RHIUpdateTexture2D(IntensityGradientTexture->GetResource()->GetTexture2DRHI(), 0, UpdateRegion,
            sizeof(FColor), reinterpret_cast<uint8_t*>(IntensityGradientPixels.GetData()));


        RHIUpdateTexture2D(HSVGradientTexture->GetResource()->GetTexture2DRHI(), 0, UpdateRegion,
            sizeof(FColor), reinterpret_cast<uint8_t*>(HSVGradientPixels.GetData()));
        RHIUpdateTexture2D(SaturationGradientTexture->GetResource()->GetTexture2DRHI(), 0, UpdateRegion,
            sizeof(FColor), reinterpret_cast<uint8_t*>(SaturationGradientPixels.GetData()));
        RHIUpdateTexture2D(TemperatureGradientTexture->GetResource()->GetTexture2DRHI(), 0, UpdateRegion,
            sizeof(FColor), reinterpret_cast<uint8_t*>(TemperatureGradientPixels.GetData()));

    };
        //EnqueueUniqueRenderCommand(l);
        ENQUEUE_RENDER_COMMAND(UpdateTextureDataCommand)(l);
        FlushRenderingCommands();
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
                .IsEnabled_Lambda([this]() {return SelectedItems.Num() != 0; })
            ]

        ];

    return Slot;
}

SVerticalBox::FSlot& SLightControlTool::LightPropertyEditor()
{
    auto& Slot = SVerticalBox::Slot();

    Slot
    .Padding(20.0f, 30.0f, 20.0f, 0.0f)
    .VAlign(VAlign_Fill)
    .HAlign(HAlign_Fill)
    [
        SNew(SHorizontalBox)
        +SHorizontalBox::Slot() // General light properties + scene parenting thingy from mock-up
        //.MaxWidth(300) // Need to see just how big this is, very much subject to change this one
        [
            SNew(SVerticalBox)
            + GeneralLightPropertyEditor()
            + LightSceneTransformEditor()
        ]
        + LightSpecificPropertyEditor()
    ];


    return Slot;
}

SVerticalBox::FSlot& SLightControlTool::GeneralLightPropertyEditor()
{
    auto& Slot = SVerticalBox::Slot();

    SVerticalBox::FSlot* IntensityNameSlot, * IntensityLumenSlot, * IntensityPercentageSlot;
    SVerticalBox::FSlot* HSVNameSlot, * HSVLumenSlot, * HSVPercentageSlot;
    SVerticalBox::FSlot* SaturationNameSlot, * SaturationLumenSlot, * SaturationPercentageSlot;
    SVerticalBox::FSlot* TemperatureNameSlot, *TemperatureLumenSlot, *TemperaturePercentageSlot;

    

    Slot
    [
        SNew(SHorizontalBox)
        +SHorizontalBox::Slot() // Intensity slider
        .VAlign(VAlign_Fill)
        [
            SNew(SBorder)
            .VAlign(VAlign_Fill)
            .HAlign(HAlign_Fill)
            .ColorAndOpacity(FLinearColor::Gray)
            [
                SNew(SVerticalBox)
                +SVerticalBox::Slot()
                .Expose(IntensityNameSlot)
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("Intensity"))
                ]
                +SVerticalBox::Slot()
                .Expose(IntensityLumenSlot)
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("300 lumens"))
                ]
                +SVerticalBox::Slot()
                [
                    SNew(SHorizontalBox)
                    + SHorizontalBox::Slot()
                    .HAlign(HAlign_Right)
                    [
                        SNew(SImage)
                        .Image(IntensityGradientBrush.Get())
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Left)
                    [
                        SNew(SSlider)
                        .Orientation(EOrientation::Orient_Vertical)
                    ]
                ]
                +SVerticalBox::Slot()
                .Expose(IntensityPercentageSlot)
                [
                    SNew(STextBlock)
                    .Justification(ETextJustify::Center)
                    .Text(FText::FromString("50%"))
                ]
            ]
        ]
        + SHorizontalBox::Slot()
            .VAlign(VAlign_Fill)
        [
            SNew(SBorder)
            .VAlign(VAlign_Fill)
            .HAlign(HAlign_Fill)
            .ColorAndOpacity(FLinearColor::Gray)
            [
                SNew(SVerticalBox)
                +SVerticalBox::Slot()
                .Expose(HSVNameSlot)
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("Hue"))
                ]
                +SVerticalBox::Slot()
                .Expose(HSVLumenSlot)
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("360"))
                ]
                +SVerticalBox::Slot()
                [
                    SNew(SHorizontalBox)
                    + SHorizontalBox::Slot()
                    .HAlign(HAlign_Right)
                    [
                        SNew(SImage)
                        .Image(HSVGradientBrush.Get())
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Left)
                    [
                        SNew(SSlider)
                        .Orientation(EOrientation::Orient_Vertical)
                    ]
                ]
                +SVerticalBox::Slot()
                .Expose(HSVPercentageSlot)
                [
                    SNew(STextBlock)
                    .Justification(ETextJustify::Center)
                    .Text(FText::FromString("50%"))
                ]
            ]
        ]
        + SHorizontalBox::Slot()
            .VAlign(VAlign_Fill)
        [
            SNew(SBorder)
            .VAlign(VAlign_Fill)
            .HAlign(HAlign_Fill)
            .ColorAndOpacity(FLinearColor::Gray)
            [
                SNew(SVerticalBox)
                +SVerticalBox::Slot()
                .Expose(SaturationNameSlot)
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("Saturation"))
                ]
                +SVerticalBox::Slot()
                .Expose(SaturationLumenSlot)
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("33%"))
                ]
                +SVerticalBox::Slot()
                [
                    SNew(SHorizontalBox)
                    + SHorizontalBox::Slot()
                    .HAlign(HAlign_Right)
                    [
                        SNew(SImage)
                        .Image(SaturationGradientBrush.Get())
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Left)
                    [
                        SNew(SSlider)
                        .Orientation(EOrientation::Orient_Vertical)
                    ]
                ]
                +SVerticalBox::Slot()
                .Expose(SaturationPercentageSlot)
                [
                    SNew(STextBlock)
                    .Justification(ETextJustify::Center)
                    .Text(FText::FromString("50%"))
                ]
            ]
        ]
        + SHorizontalBox::Slot()
            .VAlign(VAlign_Fill)
        [
            SNew(SBorder)
            .VAlign(VAlign_Fill)
            .HAlign(HAlign_Fill)
            .ColorAndOpacity(FLinearColor::Gray)
            [
                SNew(SVerticalBox)
                +SVerticalBox::Slot()
                .Expose(TemperatureNameSlot)
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("Temperature"))
                ]
                +SVerticalBox::Slot()
                .Expose(TemperatureLumenSlot)
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("3000 Kelvin"))
                ]
                +SVerticalBox::Slot()
                [
                    SNew(SHorizontalBox)
                    + SHorizontalBox::Slot()
                    .HAlign(HAlign_Right)
                    [
                        SNew(SImage)
                        .Image(TemperatureGradientBrush.Get())
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Left)
                    [
                        SNew(SSlider)
                        .Orientation(EOrientation::Orient_Vertical)
                    ]
                ]
                +SVerticalBox::Slot()
                .Expose(TemperaturePercentageSlot)
                [
                    SNew(STextBlock)
                    .Justification(ETextJustify::Center)
                    .Text(FText::FromString("71%"))
                ]
            ]
        ]

    ];

    IntensityNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    IntensityLumenSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    IntensityPercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    HSVNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    HSVLumenSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    HSVPercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    SaturationNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    SaturationLumenSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    SaturationPercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    TemperatureNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    TemperatureLumenSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    TemperaturePercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;


    return Slot;
}

SVerticalBox::FSlot& SLightControlTool::LightSceneTransformEditor()
{
    auto& Slot = SVerticalBox::Slot();
    Slot.SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    SHorizontalBox::FSlot* ButtonsSlot;

    Slot
    .Padding(0.0f, 5.0f, 0.0f, 0.0f)
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
                        .Text(FText::FromString("none"))
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
                        .Text(FText::FromString("0.0; 0.0; 0.0"))
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
                        .Text(FText::FromString("0.0; 0.0; 270.0"))
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
                        .Text(FText::FromString("1.0; 1.0; 1.0"))
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
                ]
                + SVerticalBox::Slot()
                .Padding(5.0f)
                [
                    SNew(SButton)
                    .Text(FText::FromString("Select Parent Object"))
                ]
            ]
        ]
    ];

    ButtonsSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    return Slot;
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

void SLightControlTool::UpdateLightList()
{
    TArray<AActor*> Actors;
    // Fetch Point Lights
    UGameplayStatics::GetAllActorsOfClass(GWorld, APointLight::StaticClass(), Actors);
    for (auto Light : Actors)
    {
        TSharedPtr<FTreeItem> NewItem = MakeShared<FTreeItem>();
        NewItem->OwningWidget = this;
        NewItem->Parent = nullptr;
        NewItem->Type = ETreeItemType::PointLight;
        NewItem->Name = Light->GetName();
        NewItem->PointLight = Cast<APointLight>(Light);


        TreeItems.Add(NewItem);
    }

    // Fetch Sky Lights
    UGameplayStatics::GetAllActorsOfClass(GWorld, ASkyLight::StaticClass(), Actors);
    for (auto Light : Actors)
    {
        TSharedPtr<FTreeItem> NewItem = MakeShared<FTreeItem>();
        NewItem->OwningWidget = this;
        NewItem->Parent = nullptr;
        NewItem->Type = ETreeItemType::SkyLight;
        NewItem->Name = Light->GetName();
        NewItem->SkyLight = Cast<ASkyLight>(Light);

        TreeItems.Add(NewItem);
    }

    // Fetch Directional Lights
    UGameplayStatics::GetAllActorsOfClass(GWorld, ADirectionalLight::StaticClass(), Actors);
    for (auto Light : Actors)
    {
        TSharedPtr<FTreeItem> NewItem = MakeShared<FTreeItem>();;
        NewItem->OwningWidget = this;
        NewItem->Parent = nullptr;
        NewItem->Type = ETreeItemType::DirectionalLight;
        NewItem->Name = Light->GetName();
        NewItem->DirectionalLight = Cast<ADirectionalLight>(Light);

        TreeItems.Add(NewItem);
    }

    // Fetch Spot Lights
    UGameplayStatics::GetAllActorsOfClass(GWorld, ASpotLight::StaticClass(), Actors);
    for (auto Light : Actors)
    {
        TSharedPtr<FTreeItem> NewItem = MakeShared<FTreeItem>();;
        NewItem->OwningWidget = this;
        NewItem->Parent = nullptr;
        NewItem->Type = ETreeItemType::SpotLight;
        NewItem->Name = Light->GetName();
        NewItem->SpotLight = Cast<ASpotLight>(Light);

        TreeItems.Add(NewItem);
    }

    GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Orange, "Finished adding lights to tree");
}
