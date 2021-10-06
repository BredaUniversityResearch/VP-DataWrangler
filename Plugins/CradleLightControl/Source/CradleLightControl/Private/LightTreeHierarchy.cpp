#include "LightTreeHierarchy.h"
#include "Kismet/GameplayStatics.h"

#include "Engine/SkyLight.h"
#include "Engine/PointLight.h"
#include "Engine/SpotLight.h"
#include "Engine/DirectionalLight.h"

#include "Components/LightComponent.h"
#include "Components/SkyLightComponent.h"
#include "Styling/SlateIconFinder.h"
#include "Components/PointLightComponent.h"
#include "Components/SpotLightComponent.h"

#include "Interfaces/IPluginManager.h"

#include "LightControlTool.h"

#pragma region TreeItemStruct
FTreeItem::FTreeItem(SLightTreeHierarchy* InOwningWidget, FString InName, TArray<TSharedPtr<FTreeItem>> InChildren)
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

    if (Type == ETreeItemType::PointLight)
    {
        auto Comp = PointLight->PointLightComponent;
        Intensity = Comp->Intensity / 2010.619f;       
    }    
    else if (Type == ETreeItemType::SpotLight)
    {
        auto Comp = SpotLight->SpotLightComponent;
        Intensity = Comp->Intensity / 2010.619f;
    }
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

void FTreeItem::SetLightIntensity(float NewValue)
{
    if (Type == ETreeItemType::SkyLight)
    {
        auto LightComp = SkyLight->GetLightComponent();
        LightComp->Intensity = NewValue;
    }
    else
    {
        if (Type == ETreeItemType::PointLight)
        {
            auto PointLightComp = Cast<UPointLightComponent>(PointLight->GetLightComponent());
            PointLightComp->SetIntensityUnits(ELightUnits::Lumens);
            PointLightComp->SetIntensity(NewValue * 2010.619f);            
        }
        else if (Type == ETreeItemType::SpotLight)
        {
            auto SpotLightComp = Cast<USpotLightComponent>(SpotLight->GetLightComponent());
            SpotLightComp->SetIntensityUnits(ELightUnits::Lumens);
            SpotLightComp->SetIntensity(NewValue * 2010.619f);
        }
    }
    Intensity = NewValue;
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

#pragma endregion

void SLightTreeHierarchy::Construct(const FArguments& Args)
{
    LightVerificationTimer = RegisterActiveTimer(0.5f, FWidgetActiveTimerDelegate::CreateRaw(this, &SLightTreeHierarchy::VerifyLights));


    FSlateFontInfo Font24(FCoreStyle::GetDefaultFont(), 20);

    CoreToolPtr = Args._CoreToolPtr;

    UpdateLightList();

    // SVerticalBox slots are by default dividing the space equally between each other
    // Because of this we need to expose the slot with the search bar in order to disable that for it

    SVerticalBox::FSlot* LightSearchBarSlot;
    SVerticalBox::FSlot* NewFolderButtonSlot;
    ChildSlot[
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
            [
                SNew(SButton)
                .Text(FText::FromString("Save"))
                .OnClicked(this, &SLightTreeHierarchy::SaveStateToJSON)
            ]
        + SVerticalBox::Slot()
            [
                SNew(SButton)
                .Text(FText::FromString("Load"))
                .OnClicked(this, &SLightTreeHierarchy::LoadStateFromJSON)
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
                .OnSelectionChanged(this, &SLightTreeHierarchy::SelectionCallback)
                .OnGenerateRow(this, &SLightTreeHierarchy::AddToTree)
                .OnGetChildren(this, &SLightTreeHierarchy::GetChildren)
                .OnExpansionChanged(this, &SLightTreeHierarchy::TreeExpansionCallback)
            ]
        ]
        +SVerticalBox::Slot()
        .VAlign(VAlign_Bottom)
        .Padding(0.0f, 10.0f, 0.0f, 0.0f)
        .Expose(NewFolderButtonSlot)
        [
            SNew(SButton)
            .Text(FText::FromString("Add New Group"))
            .OnClicked_Raw(this, &SLightTreeHierarchy::AddFolderToTree)
        ]
    ];


    LightSearchBarSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    NewFolderButtonSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;


    for (auto& TreeItem : TreeItems)
    {
        Tree->SetItemExpansion(TreeItem, true);
    }
}

void SLightTreeHierarchy::PreDestroy()
{
    UnRegisterActiveTimer(LightVerificationTimer.ToSharedRef());

}

void SLightTreeHierarchy::OnActorSpawned(AActor* Actor)
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

TSharedRef<ITableRow> SLightTreeHierarchy::AddToTree(TSharedPtr<FTreeItem> Item,
                                                     const TSharedRef<STableViewBase>& OwnerTable)
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

void SLightTreeHierarchy::GetChildren(TSharedPtr<FTreeItem> Item, TArray<TSharedPtr<FTreeItem>>& Children)
{
    Children.Append(Item->Children);

}

void SLightTreeHierarchy::SelectionCallback(TSharedPtr<FTreeItem> Item, ESelectInfo::Type SelectType)
{

    SelectedItems = Tree->GetSelectedItems();
    if (SelectedItems.Num())
    {
        int Index;
        LightsUnderSelection.Empty();
        for (auto& Selected : SelectedItems)
        {
            Selected->GetLights(LightsUnderSelection);
        }

        if (SelectionMasterLight == nullptr || !LightsUnderSelection.Find(SelectionMasterLight, Index))
        {
            SelectionMasterLight = LightsUnderSelection[0];
        }
    }
    else
        SelectionMasterLight = nullptr;
    CoreToolPtr->OnTreeSelectionChanged();
}

FReply SLightTreeHierarchy::AddFolderToTree()
{
    TSharedPtr<FTreeItem> NewFolder = AddTreeItem(true);
    NewFolder->Name = "New Group";

    TreeItems.Add(NewFolder);

    Tree->RequestTreeRefresh();

    return FReply::Handled();
}

void SLightTreeHierarchy::TreeExpansionCallback(TSharedPtr<FTreeItem> Item, bool bExpanded)
{
    Item->bExpanded = bExpanded;
}

TSharedPtr<FTreeItem> SLightTreeHierarchy::AddTreeItem(bool bIsFolder)
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

EActiveTimerReturnType SLightTreeHierarchy::VerifyLights(double, float)
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

void SLightTreeHierarchy::UpdateLightList()
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
}


FReply SLightTreeHierarchy::SaveStateToJSON()
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

FReply SLightTreeHierarchy::LoadStateFromJSON()
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


