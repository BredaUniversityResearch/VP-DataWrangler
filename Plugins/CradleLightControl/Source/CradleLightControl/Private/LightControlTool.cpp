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

#include "Styling/SlateIconFinder.h"
#include "Engine/SkyLight.h"
#include "Engine/PointLight.h"
#include "Engine/SpotLight.h"
#include "Engine/DirectionalLight.h"


FTreeItem::FTreeItem(SLightControlTool* InOwningWidget, FString InName, TArray<TSharedPtr<FTreeItem>> InChildren)
    : Name(InName)
    , Children(InChildren)
    , OwningWidget(InOwningWidget)
    , bInRename(false)
    , ActorPtr(nullptr)
{
}

ECheckBoxState FTreeItem::IsLightEnabled() const
{
    bool AllOff = true, AllOn = true;

    if (Type != Folder && !IsValid(SkyLight))
        return ECheckBoxState::Undetermined;

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
    if (Type != Folder && !IsValid(SkyLight))
        return;

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

    static auto Icon = *FSlateIconFinder::FindIconBrushForClass(APointLight::StaticClass());
    Icon.SetImageSize(FVector2D(16.0f, 16.0f));
    Icon.DrawAs = ESlateBrushDrawType::Box;
    Icon.Margin = FVector2D(0.0f, 0.0f);
    auto TintColor = FLinearColor(0.2f, 0.2f, 0.2f, 0.5f);
    Icon.TintColor = FSlateColor(TintColor);

    SHorizontalBox::FSlot* IconSlot;

    TableRowBox->SetContent(
        SNew(SHorizontalBox)
        + SHorizontalBox::Slot()
        .Expose(IconSlot)
        [
            SNew(SImage)
            .Image(&Icon)            
        ]
        + SHorizontalBox::Slot()
        [
            SAssignNew(TextSlot, SBox)    
        ]
        + CheckBoxSlot);

    IconSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

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

TSharedPtr<FJsonValue> FTreeItem::SaveToJson()
{
    TSharedPtr<FJsonObject> Item = MakeShared<FJsonObject>();

    auto Enabled = IsLightEnabled();

    int ItemState = 0;// Unchecked
    if (Enabled == ECheckBoxState::Undetermined)
        ItemState = 1;
    else if (Enabled == ECheckBoxState::Checked)
        ItemState = 2;

    Item->SetStringField("Name", Name);
    Item->SetNumberField("Type", Type);
    Item->SetBoolField("Expanded", bExpanded);
    if (Type != Folder)
    {
        Item->SetStringField("RelatedLightName", SkyLight->GetName());
        Item->SetNumberField("State", ItemState);
    }

    TArray<TSharedPtr<FJsonValue>> ChildrenJson;

    for (auto Child : Children)
    {
        ChildrenJson.Add(Child->SaveToJson());
    }

    Item->SetArrayField("Children", ChildrenJson);

    TSharedPtr<FJsonValue> JsonValue = MakeShared<FJsonValueObject>(Item);
    return JsonValue;
}

bool FTreeItem::LoadFromJson(TSharedPtr<FJsonObject> JsonObject)
{
    Name = JsonObject->GetStringField("Name");
    Type = StaticCast<ETreeItemType>(JsonObject->GetNumberField("Type"));
    bExpanded = JsonObject->GetBoolField("Expanded");
    if (Type != Folder)
    {
        if (!GWorld)
            return false;

        auto LightName = JsonObject->GetStringField("RelatedLightName");

        UClass* ClassToFetch = AActor::StaticClass();

        switch (Type)
        {
        case ETreeItemType::SkyLight:
            ClassToFetch = ASkyLight::StaticClass();
            break;
        case ETreeItemType::SpotLight:
            ClassToFetch = ASpotLight::StaticClass();
            break;
        case ETreeItemType::DirectionalLight:
            ClassToFetch = ADirectionalLight::StaticClass();
            break;
        case ETreeItemType::PointLight:
            ClassToFetch = APointLight::StaticClass();
            break;
        default:
            return false;
        }

        TArray<AActor*> Actors;
        UGameplayStatics::GetAllActorsOfClass(GWorld, ClassToFetch, Actors);

        ActorPtr = *Actors.FindByPredicate([&LightName](AActor* Element)
            {
                return Element && Element->GetName() == LightName;
            });

        auto State = JsonObject->GetBoolField("State");

        OnCheck(State == 0 ? ECheckBoxState::Unchecked : ECheckBoxState::Checked);
    }
    else
    {
        auto JsonChildren = JsonObject->GetArrayField("Children");

        auto ChildrenLoadingSuccess = true;
        for (auto Child : JsonChildren)
        {
            const TSharedPtr<FJsonObject>* ChildObjectPtr;
            auto Success = Child->TryGetObject(ChildObjectPtr);
            auto ChildObject = *ChildObjectPtr;
            _ASSERT(Success);
            int ChildType = ChildObject->GetNumberField("Type");
            auto ChildItem = OwningWidget->AddTreeItem(ChildType == 0);

            ChildItem->Parent = SharedThis(this);

            ChildrenLoadingSuccess &= ChildItem->LoadFromJson(ChildObject);
            if (!ChildrenLoadingSuccess)
                break;

            Children.Add(ChildItem);
        }
        return ChildrenLoadingSuccess;
    }
    

    return true;
}

void FTreeItem::ExpandInTree()
{
    OwningWidget->Tree->SetItemExpansion(SharedThis(this), bExpanded);

    for (auto Child : Children)
    {
        Child->ExpandInTree();
    }
}

void FTreeItem::FetchDataFromLight()
{
    _ASSERT(Type != Folder);

    FLinearColor RGB;

    if (Type == ETreeItemType::SkyLight)
    {
        RGB = SkyLight->GetLightComponent()->GetLightColor();
        
    }
    else
    {
        ALight* LightPtr = Cast<ALight>(PointLight);
        RGB = LightPtr->GetLightColor();
    }
    auto HSV = RGB.LinearRGBToHSV();
    Saturation = HSV.G;

    // If Saturation is 0, the color is white. The RGB => HSV conversion calculates the Hue to be 0 in that case, even if it's not supposed to be.
    // Do this to preserve the Hue previously used rather than it getting reset to 0.
    if (Saturation == 0.0f)
        Hue = HSV.R; 
    Value = HSV.B;
}

void FTreeItem::UpdateLightColor()
{
    auto NewColor = FLinearColor::MakeFromHSV8(StaticCast<uint8>(Hue * 255.0f), StaticCast<uint8>(Saturation * 255.0f), 255);
    UpdateLightColor(NewColor);
}

void FTreeItem::UpdateLightColor(FLinearColor& Color)
{
    if (Type == Folder)
    {
        // IDK how i am gonna do this crap
    }
    else if (Type == ETreeItemType::SkyLight)
    {
        SkyLight->GetLightComponent()->SetLightColor(Color);
    }
    else
    {
        auto LightPtr = Cast<ALight>(PointLight);
        LightPtr->SetLightColor(Color);
    }
}

void FTreeItem::GetLights(TArray<TSharedPtr<FTreeItem>>& Array)
{
    if (Type == Folder)
    {
        for (auto& Child : Children)
            Child->GetLights(Array);
    }
    else
    {
        Array.Add(SharedThis(this));
    }
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
        OwningWidget->Tree->SetItemExpansion(Destination, true);
    }
    else
    {

        auto Destination = OwningWidget->AddTreeItem(true);
        Destination->Name = Name + " Group";

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

        OwningWidget->Tree->SetItemExpansion(Destination, true);
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

    LightVerificationTimer = RegisterActiveTimer(0.5f, FWidgetActiveTimerDelegate::CreateRaw(this, &SLightControlTool::VerifyLights));
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

    SVerticalBox::FSlot* shit;

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
            .Value(0.50f)
            //.SizeRule(SSplitter::ESizeRule::SizeToContent)
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
                        SNew(SSearchBox)
                        // Search bar for light
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
                    .Expose(shit)
                    [
                        SNew(SButton)
                        .Text(FText::FromString("Save"))
                        .OnClicked(this, &SLightControlTool::SaveStateToJSON)
                    ]
                + SVerticalBox::Slot()
                    [
                        SNew(SButton)
                        .Text(FText::FromString("Load"))
                        .OnClicked(this, &SLightControlTool::LoadStateFromJSON)
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
                        .OnExpansionChanged(this, &SLightControlTool::TreeExpansionCallback)
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
    //SplitterSlot->SizeValue = FMath::Min(180.0f, SplitterSlot->SizeValue.Get());

    shit->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

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
    //PreDestroy();
}

void SLightControlTool::PreDestroy()
{
    //if (IntensityGradientTexture)
    {
        IntensityGradientTexture->ConditionalBeginDestroy();
        IntensityGradientTexture->RemoveFromRoot();
        HSVGradientTexture->ConditionalBeginDestroy();
        HSVGradientTexture->RemoveFromRoot();
        DefaultSaturationGradientTexture->ConditionalBeginDestroy();
        DefaultSaturationGradientTexture->RemoveFromRoot();
        SaturationGradientTexture->ConditionalBeginDestroy();
        SaturationGradientTexture->RemoveFromRoot();
        TemperatureGradientTexture->ConditionalBeginDestroy();
        TemperatureGradientTexture->RemoveFromRoot();
        //IntensityGradientTexture.Reset();        
    }


    UnRegisterActiveTimer(LightVerificationTimer.ToSharedRef());
    GWorld->RemoveOnActorSpawnedHandler(ActorSpawnedListenerHandle);
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
    UpdateExtraLightDetailBox();
    if (SelectedItems.Num())
    {
        int Index;
        SelectedLightLeafs.Empty();
        for (auto& Selected : SelectedItems)
        {
            Selected->GetLights(SelectedLightLeafs);
        }

        if (MasterLight == nullptr || !SelectedLightLeafs.Find(MasterLight, Index))
        {
            MasterLight = SelectedLightLeafs[0];
        }
        UpdateSaturationGradient(SelectedItems[0]->Hue);
    }
    else
        MasterLight = nullptr;
}

FReply SLightControlTool::AddFolderToTree()
{
    TSharedPtr<FTreeItem> NewFolder = AddTreeItem(true);
    NewFolder->Name = "New Group";

    TreeItems.Add(NewFolder);

    Tree->RequestTreeRefresh();

    return FReply::Handled();
}

void SLightControlTool::TreeExpansionCallback(TSharedPtr<FTreeItem> Item, bool bExpanded)
{
    Item->bExpanded = bExpanded;
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

#include "LightLifeSpanTracker.h"

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
        auto NewItem = AddTreeItem();
        NewItem->Type = Type;
        NewItem->Name = Actor->GetName();


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
        NewItem->FetchDataFromLight();

        TreeItems.Add(NewItem);

        Tree->RequestTreeRefresh();
    }
}

TSharedPtr<FTreeItem> SLightControlTool::AddTreeItem(bool bIsFolder)
{
    auto Item = MakeShared<FTreeItem>();
    Item->OwningWidget = this;
    Item->Parent = nullptr;


    //TreeItems.Add(Item);
    if (bIsFolder)
    {
        Item->Type = Folder;
    }
    else // Do this so that only actual lights which might be deleted in the editor are checked for validity
        ListOfLightItems.Add(&Item.Get());

    return Item;
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

EActiveTimerReturnType SLightControlTool::VerifyLights(double, float)
{
    if (bCurrentlyLoading)
        return EActiveTimerReturnType::Continue;
    GEngine->AddOnScreenDebugMessage(-1, 0.5f, FColor::Blue, "Cleaning invalid lights");
    TArray<FTreeItem*> ToRemove;
    for (auto Item : ListOfLightItems)
    {
        if (!Item->ActorPtr || !IsValid(Item->SkyLight))
        {
            if (Item->Parent)
                Item->Parent->Children.Remove(Item->AsShared());
            else
                TreeItems.Remove(Item->AsShared());


            ToRemove.Add(Item);
        }
        else
        {
            //Item->FetchDataFromLight();
        }
    }

    for (auto Item : ToRemove)
    {
        ListOfLightItems.Remove(Item);
    }

    if (ToRemove.Num())
    {
        Tree->RequestTreeRefresh();
    }

    return EActiveTimerReturnType::Continue;
}

void SLightControlTool::LoadResources()
{

    
    GenerateTextures();


    


    const FVector2D GradientSize(20.0f, 256.0f);

    IntensityGradientBrush = MakeShared<FSlateImageBrush>(IntensityGradientTexture, GradientSize);
    HSVGradientBrush = MakeShared<FSlateImageBrush>(HSVGradientTexture, GradientSize);
    DefaultSaturationGradientBrush = MakeShared<FSlateImageBrush>(DefaultSaturationGradientTexture, GradientSize);
    SaturationGradientBrush = MakeShared<FSlateImageBrush>(SaturationGradientTexture, GradientSize);
    TemperatureGradientBrush = MakeShared<FSlateImageBrush>(TemperatureGradientTexture, GradientSize);

}

void SLightControlTool::GenerateTextures()
{

    IntensityGradientTexture = MakeGradientTexture();
    HSVGradientTexture = MakeGradientTexture();
    DefaultSaturationGradientTexture = MakeGradientTexture();
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
            FColor::Magenta,
            FColor::Blue,
            FColor::Cyan,
            FColor::Green,
            FColor::Yellow,
            FColor::Red
        });

        TArray<FColor> SaturationGradientPixels = LinearGradient(TArray<FColor>{
            FColor::Red,
            FColor::White
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
        RHIUpdateTexture2D(DefaultSaturationGradientTexture->GetResource()->GetTexture2DRHI(), 0, UpdateRegion,
            sizeof(FColor), reinterpret_cast<uint8_t*>(SaturationGradientPixels.GetData()));
        RHIUpdateTexture2D(SaturationGradientTexture->GetResource()->GetTexture2DRHI(), 0, UpdateRegion, // Initialize the non-default saturation texture the same as the default one
            sizeof(FColor), reinterpret_cast<uint8_t*>(SaturationGradientPixels.GetData()));
        RHIUpdateTexture2D(TemperatureGradientTexture->GetResource()->GetTexture2DRHI(), 0, UpdateRegion,
            sizeof(FColor), reinterpret_cast<uint8_t*>(TemperatureGradientPixels.GetData()));

    };
        //EnqueueUniqueRenderCommand(l);
        ENQUEUE_RENDER_COMMAND(UpdateTextureDataCommand)(l);
        FlushRenderingCommands();
}

void SLightControlTool::UpdateSaturationGradient(float NewHue)
{
    ENQUEUE_RENDER_COMMAND(UpdateTextureDataCommand)([this, NewHue](FRHICommandListImmediate& RHICmdList)
        {
            auto UpdateRegion = FUpdateTextureRegion2D(0, 0, 0, 0, 1, 256);

            auto C = FLinearColor::MakeFromHSV8(StaticCast<uint8>(NewHue * 255.0f), 255, 255).ToFColor(false);
            auto NewGradient = LinearGradient(TArray<FColor>{
                C,
                    FColor::White
            });

            RHIUpdateTexture2D(SaturationGradientTexture->GetResource()->GetTexture2DRHI(), 0, UpdateRegion, sizeof(FColor), reinterpret_cast<uint8_t*>(NewGradient.GetData()));
        });
}

const FSlateBrush* SLightControlTool::GetSaturationGradientBrush() const
{
    if (SelectedItems.Num())
    {
        return SaturationGradientBrush.Get();
    }
    return DefaultSaturationGradientBrush.Get();
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
            + GeneralLightPropertyEditor()
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
    if (SelectedItems.Num())
    {
        if (SelectedItems.Num() > 1 || SelectedItems[0]->Type == Folder)
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

SVerticalBox::FSlot& SLightControlTool::GeneralLightPropertyEditor()
{
    auto& Slot = SVerticalBox::Slot();

    SVerticalBox::FSlot* IntensityNameSlot, *IntensityValueSlot, *IntensityPercentageSlot;
    SVerticalBox::FSlot* HueNameSlot, *HueValueSlot, *HuePercentageSlot;
    SVerticalBox::FSlot* SaturationNameSlot, *SaturationValueSlot, *SaturationPercentageSlot;
    SVerticalBox::FSlot* TemperatureNameSlot, *TemperatureValueSlot, *TemperaturePercentageSlot;


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
                .Expose(IntensityValueSlot)
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("300 lumens"))
                ]
                +SVerticalBox::Slot()
                [
                    SNew(SHorizontalBox)
                    + SHorizontalBox::Slot()
                    .Padding(3.0f, 0.0f)
                    .HAlign(HAlign_Right)
                    [
                        SNew(SBorder)
                        [
                            SNew(SImage)
                            .Image(IntensityGradientBrush.Get())
                        ]
                    ]
                    +SHorizontalBox::Slot()
                    .Padding(3.0f, 0.0f)
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
                .Expose(HueNameSlot)
                [
                    SNew(STextBlock)
                    .Justification(ETextJustify::Center)
                    .Text(FText::FromString("Hue"))
                ]
                +SVerticalBox::Slot()
                .Expose(HueValueSlot)
                [
                    SNew(STextBlock)
                    .Justification(ETextJustify::Center)
                    .Text(this, &SLightControlTool::GetHueValueText)
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
                        .Value(this, &SLightControlTool::GetHueValue)
                        .OnValueChanged_Raw(this, &SLightControlTool::OnHueValueChanged)
                    ]
                ]
                +SVerticalBox::Slot()
                .Expose(HuePercentageSlot)
                [
                    SNew(STextBlock)
                    .Justification(ETextJustify::Center)
                    .Text(this, &SLightControlTool::GetHuePercentage)
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
                    .Justification(ETextJustify::Center)
            .Text(FText::FromString("Saturation"))
                ]
                +SVerticalBox::Slot()
                .Expose(SaturationValueSlot)
                [
                    SNew(STextBlock)
                    .Justification(ETextJustify::Center)
                    .Text(this, &SLightControlTool::GetSaturationValueText)
                ]
                +SVerticalBox::Slot()
                [
                    SNew(SHorizontalBox)
                    + SHorizontalBox::Slot()
                    .HAlign(HAlign_Right)
                    [
                        SNew(SImage)
                        .Image_Raw(this, &SLightControlTool::GetSaturationGradientBrush)
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Left)
                    [
                        SNew(SSlider)
                        .Orientation(EOrientation::Orient_Vertical)
                        .Value_Raw(this, &SLightControlTool::GetSaturationValue)
                        .OnValueChanged_Raw(this, &SLightControlTool::OnSaturationValueChanged)
                    ]
                ]
                +SVerticalBox::Slot()
                .Expose(SaturationPercentageSlot)
                [
                    SNew(STextBlock)
                    .Justification(ETextJustify::Center)
                    .Text(this, &SLightControlTool::GetSaturationValueText) // The content is the same as the value text
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
                .Expose(TemperatureValueSlot)
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
    IntensityValueSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    IntensityPercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    HueNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    HueValueSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    HuePercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    SaturationNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    SaturationValueSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    SaturationPercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    TemperatureNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    TemperatureValueSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    TemperaturePercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;


    return Slot;
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
                    .OnClicked(this, &SLightControlTool::SelectItem)
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

FReply SLightControlTool::SelectItem()
{
    if (SelectedItems.Num() && SelectedItems[0]->Type != Folder)
    {
        GEditor->SelectNone(true, true);
        GEditor->SelectActor(SelectedItems[0]->ActorPtr, true, true, false, true);        
    }

    return FReply::Handled();
}

FReply SLightControlTool::SelectItemParent()
{
    GEditor->SelectNone(true, true);
    GEditor->SelectActor(SelectedItems[0]->ActorPtr->GetAttachParentActor(), true, true, false, true);

    return FReply::Handled();
}

bool SLightControlTool::SelectItemParentButtonEnable() const
{
    return SelectedItems.Num() && SelectedItems[0]->Type != Folder && SelectedItems[0]->ActorPtr->GetAttachParentActor();
}

FText SLightControlTool::GetItemParentName() const
{
    if (SelectedItems.Num() && SelectedItems[0]->Type != Folder && SelectedItems[0]->ActorPtr->GetAttachParentActor())
    {
        return FText::FromString(SelectedItems[0]->ActorPtr->GetAttachParentActor()->GetName());
    }
    return FText::FromString("None");
}

FText SLightControlTool::GetItemPosition() const
{
    if (SelectedItems.Num() && SelectedItems[0]->Type != Folder)
    {
        return FText::FromString(SelectedItems[0]->ActorPtr->GetActorLocation().ToString());
    }
    return FText::FromString("");
}

FText SLightControlTool::GetItemRotation() const
{
    if (SelectedItems.Num() && SelectedItems[0]->Type != Folder)
    {
        return FText::FromString(SelectedItems[0]->ActorPtr->GetActorRotation().ToString());
    }
    return FText::FromString("");
}

FText SLightControlTool::GetItemScale() const
{
    if (SelectedItems.Num() && SelectedItems[0]->Type != Folder)
    {
        return FText::FromString(SelectedItems[0]->ActorPtr->GetActorScale().ToString());
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
                    .OptionsSource(&SelectedLightLeafs)
                    .OnGenerateWidget(this, &SLightControlTool::GroupControlDropDownLabel)
                    .OnSelectionChanged(this, &SLightControlTool::GroupControlDropDownSelection)
                    .InitiallySelectedItem(MasterLight)[
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
    MasterLight = Item;
}

FText SLightControlTool::GroupControlDropDownDefaultLabel() const
{
    if (MasterLight)
    {
        return FText::FromString(MasterLight->Name);
    }
    return FText::FromString("");
}

FText SLightControlTool::GroupControlLightList() const
{
    FString LightList = SelectedLightLeafs[0]->Name;

    for (size_t i = 1; i < SelectedLightLeafs.Num(); i++)
    {
        LightList += ", ";
        LightList += SelectedLightLeafs[i]->Name;
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

void SLightControlTool::OnHueValueChanged(float Value)
{
    for (auto SelectedItem : SelectedItems)
    {
        SelectedItem->Hue = Value;
        SelectedItem->UpdateLightColor();
        UpdateSaturationGradient(Value);
    }
}

FText SLightControlTool::GetHueValueText() const
{
    FString Res = "0";
    if (MasterLight)
    {
        Res = FString::FormatAsNumber(MasterLight->Hue * 360.0f);
    }
    return FText::FromString(Res);
}

float SLightControlTool::GetHueValue() const
{
    if (MasterLight)
    {
        return MasterLight->Hue;
    }
    return 0;
}

FText SLightControlTool::GetHuePercentage() const
{
    FString Res = "0%";
    if (MasterLight)
    {
        Res = FString::FormatAsNumber(MasterLight->Hue * 100.0f) + "%";
    }
    return FText::FromString(Res);
}

void SLightControlTool::OnSaturationValueChanged(float Value)
{
    for (auto SelectedItem : SelectedItems)
    {
        SelectedItem->Saturation = Value;
        SelectedItem->UpdateLightColor();
    }
}

FText SLightControlTool::GetSaturationValueText() const
{
    FString Res = "0%";
    if (MasterLight)
    {
        Res = FString::FormatAsNumber(MasterLight->Saturation * 100.0f) + "%";
    }
    return FText::FromString(Res);
}

float SLightControlTool::GetSaturationValue() const
{
    if (MasterLight)
    {
        return MasterLight->Saturation;
    }
    return 0.0f;
}

FReply SLightControlTool::SaveStateToJSON()
{
    TArray<TSharedPtr<FJsonValue>> TreeItemsJSON;

    for (auto TreeItem : TreeItems)
    {
        TreeItemsJSON.Add(TreeItem->SaveToJson());
    }
    
    TSharedPtr<FJsonObject> RootObject = MakeShared<FJsonObject>();

    RootObject->SetArrayField("TreeElements", TreeItemsJSON);

    FString Output;
    TSharedRef<TJsonWriter<>> Writer = TJsonWriterFactory<>::Create(&Output);
    FJsonSerializer::Serialize(RootObject.ToSharedRef(), Writer);

    auto ThisPlugin = IPluginManager::Get().FindPlugin("CradleLightControl");
    auto Content = ThisPlugin->GetContentDir();
    FFileHelper::SaveStringToFile(Output, *(Content + "\\TestFile.json"));
    /*FArchive* Archive = 
    TJsonWriter<char> Writer();*/

    return FReply::Handled();
}

FReply SLightControlTool::LoadStateFromJSON()
{
    bCurrentlyLoading = true;
    FString Input;
    auto ThisPlugin = IPluginManager::Get().FindPlugin("CradleLightControl");
    auto Content = ThisPlugin->GetContentDir();
    if (FFileHelper::LoadFileToString(Input, *(Content + "\\TestFile.json")))
    {
        TreeItems.Empty();
        ListOfLightItems.Empty();
        TSharedPtr<FJsonObject> JsonRoot;
        TSharedRef<TJsonReader<>> JsonReader = TJsonReaderFactory<>::Create(Input);
        FJsonSerializer::Deserialize(JsonReader, JsonRoot);

        for (auto TreeElement : JsonRoot->GetArrayField("TreeElements"))
        {
            const TSharedPtr<FJsonObject>* TreeElementObjectPtr;
            auto Success = TreeElement->TryGetObject(TreeElementObjectPtr);
            auto TreeElementObject = *TreeElementObjectPtr;
            _ASSERT(Success);
            int Type = TreeElementObject->GetNumberField("Type");
            auto Item = AddTreeItem(Type == 0); // If Type is 0, this element is a folder, so we add it as a folder
            Item->LoadFromJson(TreeElementObject);

            TreeItems.Add(Item);
        }
        Tree->RequestTreeRefresh();

        for (auto TreeItem : TreeItems)
        {
            TreeItem->ExpandInTree();
        }
    }
    bCurrentlyLoading = false;
    return FReply::Handled();
}

void SLightControlTool::UpdateLightList()
{
    TArray<AActor*> Actors;
    // Fetch Point Lights
    UGameplayStatics::GetAllActorsOfClass(GWorld, APointLight::StaticClass(), Actors);
    for (auto Light : Actors)
    {
        TSharedPtr<FTreeItem> NewItem = AddTreeItem();
        NewItem->Type = ETreeItemType::PointLight;
        NewItem->Name = Light->GetName();
        NewItem->PointLight = Cast<APointLight>(Light);
        NewItem->FetchDataFromLight();

        TreeItems.Add(NewItem);

    }

    // Fetch Sky Lights
    UGameplayStatics::GetAllActorsOfClass(GWorld, ASkyLight::StaticClass(), Actors);
    for (auto Light : Actors)
    {
        TSharedPtr<FTreeItem> NewItem = AddTreeItem();
        NewItem->Type = ETreeItemType::SkyLight;
        NewItem->Name = Light->GetName();
        NewItem->SkyLight = Cast<ASkyLight>(Light);
        NewItem->FetchDataFromLight();

        TreeItems.Add(NewItem);
    }

    // Fetch Directional Lights
    UGameplayStatics::GetAllActorsOfClass(GWorld, ADirectionalLight::StaticClass(), Actors);
    for (auto Light : Actors)
    {
        TSharedPtr<FTreeItem> NewItem = AddTreeItem();
        NewItem->Type = ETreeItemType::DirectionalLight;
        NewItem->Name = Light->GetName();
        NewItem->DirectionalLight = Cast<ADirectionalLight>(Light);
        NewItem->FetchDataFromLight();

        TreeItems.Add(NewItem);
    }

    // Fetch Spot Lights
    UGameplayStatics::GetAllActorsOfClass(GWorld, ASpotLight::StaticClass(), Actors);
    for (auto Light : Actors)
    {
        TSharedPtr<FTreeItem> NewItem = AddTreeItem();
        NewItem->Type = ETreeItemType::SpotLight;
        NewItem->Name = Light->GetName();
        NewItem->SpotLight = Cast<ASpotLight>(Light);
        NewItem->FetchDataFromLight();

        TreeItems.Add(NewItem);
    }

    GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Orange, "Finished adding lights to tree");
}
