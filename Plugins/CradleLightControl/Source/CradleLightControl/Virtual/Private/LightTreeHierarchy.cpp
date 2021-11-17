#include "LightTreeHierarchy.h"
#include "Kismet/GameplayStatics.h"

#include "Engine/SkyLight.h"
#include "Engine/PointLight.h"
#include "Engine/SpotLight.h"
#include "Engine/DirectionalLight.h"

#include "Widgets/Layout/SScaleBox.h"

#include "Styling/SlateIconFinder.h"

#include "LightControlTool.h"

#include "ToolData.h"
#include "VirtualLight.h"

#pragma region TreeItemStruct

//
//ECheckBoxState ULightTreeItem::IsLightEnabled() const
//{
//    bool AllOff = true, AllOn = true;
//
//    if (Type != Folder && !IsValid(SkyLight))
//        return ECheckBoxState::Undetermined;
//
//    switch (Type)
//    {
//    case ETreeItemType::SkyLight:
//        return SkyLight->GetLightComponent()->IsVisible() ? ECheckBoxState::Checked : ECheckBoxState::Unchecked;
//    case ETreeItemType::SpotLight:
//        return SpotLight->GetLightComponent()->IsVisible() ? ECheckBoxState::Checked : ECheckBoxState::Unchecked;
//    case ETreeItemType::DirectionalLight:
//        return DirectionalLight->GetLightComponent()->IsVisible() ? ECheckBoxState::Checked : ECheckBoxState::Unchecked;
//    case ETreeItemType::PointLight:
//        return PointLight->GetLightComponent()->IsVisible() ? ECheckBoxState::Checked : ECheckBoxState::Unchecked;
//    case ETreeItemType::Folder:
//        for (auto& Child : Children)
//        {
//            auto State = Child->IsLightEnabled();
//            if (State == ECheckBoxState::Checked)
//                AllOff = false;
//            else if (State == ECheckBoxState::Unchecked)
//                AllOn = false;
//            else if (State == ECheckBoxState::Undetermined)
//                return ECheckBoxState::Undetermined;
//
//            if (!AllOff && !AllOn)
//                return ECheckBoxState::Undetermined;
//        }
//
//        if (AllOn)
//            return ECheckBoxState::Checked;
//        else
//            return ECheckBoxState::Unchecked;
//
//
//    default:
//        return ECheckBoxState::Undetermined;
//    }
//}
//
//void ULightTreeItem::OnCheck(ECheckBoxState NewState)
//{
//    bool B = false;
//    if (NewState == ECheckBoxState::Checked)
//        B = true;
//    if (Type != Folder && !IsValid(ActorPtr))
//        return;
//
//    GEditor->BeginTransaction(FText::FromString(Name + " State change"));
//
//    SetEnabled(B);
//
//    GEditor->EndTransaction();
//}
//
//
//void ULightTreeItem::PostTransacted(const FTransactionObjectEvent& TransactionEvent)
//{
//    UObject::PostTransacted(TransactionEvent);
//    if (TransactionEvent.GetEventType() == ETransactionObjectEventType::UndoRedo)
//    {
//        if (Type != Folder)
//            OwningWidget->CoreToolPtr->GetLightPropertyEditor().Pin()->UpdateSaturationGradient(OwningWidget->TransactionalVariables->SelectionMasterLight->Hue);
//        else
//           OwningWidget->Tree->RequestTreeRefresh();
//
//        GenerateTableRow();
//    }
//}
//
//
//void ULightTreeItem::FetchDataFromLight()
//{
//    _ASSERT(Type != Folder);
//
//    FLinearColor RGB;
//
//    Intensity = 0.0f;
//    Saturation = 0.0f;
//    Temperature = 0.0f;
//
//    if (Type == ETreeItemType::SkyLight)
//    {
//        RGB = SkyLight->GetLightComponent()->GetLightColor();
//
//    }
//    else
//    {
//        ALight* LightPtr = Cast<ALight>(PointLight);
//        RGB = LightPtr->GetLightColor();
//    }
//    auto HSV = RGB.LinearRGBToHSV();
//    Saturation = HSV.G;
//
//    // If Saturation is 0, the color is white. The RGB => HSV conversion calculates the Hue to be 0 in that case, even if it's not supposed to be.
//    // Do this to preserve the Hue previously used rather than it getting reset to 0.
//    if (Saturation != 0.0f)
//        Hue = HSV.R;
//
//    if (Type == ETreeItemType::PointLight)
//    {
//        auto Comp = PointLight->PointLightComponent;
//        Intensity = Comp->Intensity;       
//    }    
//    else if (Type == ETreeItemType::SpotLight)
//    {
//        auto Comp = SpotLight->SpotLightComponent;
//        Intensity = Comp->Intensity;
//    }
//
//    if (Type != ETreeItemType::SkyLight)
//    {
//        auto LightPtr = Cast<ALight>(ActorPtr);
//        auto LightComp = LightPtr->GetLightComponent();
//        bUseTemperature = LightComp->bUseTemperature;
//        Temperature = LightComp->Temperature;
//
//        bCastShadows = LightComp->CastShadows;
//    }
//    else
//    {
//        bCastShadows = SkyLight->GetLightComponent()->CastShadows;
//    }
//
//    auto CurrentFwd = FQuat::MakeFromEuler(FVector(0.0f, Vertical, Horizontal)).GetForwardVector();
//    auto ActorQuat = ActorPtr->GetTransform().GetRotation().GetNormalized();
//    auto ActorFwd = ActorQuat.GetForwardVector();
//
//    if (CurrentFwd.Equals(ActorFwd))
//    {
//        auto Euler = ActorQuat.Euler();
//        Horizontal = Euler.Z;
//        Vertical = Euler.Y;
//    }
//    
//
//    if (Type == ETreeItemType::SpotLight)
//    {
//        InnerAngle = SpotLight->SpotLightComponent->InnerConeAngle;
//        OuterAngle = SpotLight->SpotLightComponent->OuterConeAngle;
//    }
//    UpdateDMX();
//}
//
//void ULightTreeItem::UpdateLightColor()
//{
//    auto NewColor = FLinearColor::MakeFromHSV8(StaticCast<uint8>(Hue / 360.0f * 255.0f), StaticCast<uint8>(Saturation * 255.0f), 255);
//    UpdateLightColor(NewColor);
//}
//
//void ULightTreeItem::UpdateLightColor(FLinearColor& Color)
//{
//    if (Type == ETreeItemType::SkyLight)
//    {
//        SkyLight->GetLightComponent()->SetLightColor(Color);
//        SkyLight->GetLightComponent()->UpdateLightSpriteTexture();
//    }
//    else
//    {
//        auto LightPtr = Cast<ALight>(PointLight);
//        LightPtr->SetLightColor(Color);
//        LightPtr->GetLightComponent()->UpdateLightSpriteTexture();
//    }
//    UpdateDMX();
//}
//
//void ULightTreeItem::UpdateDMX()
//{
//    if (DMXProperties.bUseDmx && DMXProperties.OutputPort && DMXProperties.DataConverter)
//    {
//        DMXProperties.DataConverter->Channels.Empty();
//        DMXProperties.DataConverter->StartingChannel = DMXProperties.StartingChannel;
//        DMXProperties.DataConverter->Convert(this);
//
//        //auto& Channels = DMXProperties.Channels;
//        //auto Start = DMXProperties.StartingChannel;
//
//        DMXProperties.OutputPort->SendDMX(1, DMXProperties.DataConverter->Channels);
//    }
//}
//



#pragma endregion
//
//void UTreeTransactionalVariables::PostTransacted(const FTransactionObjectEvent& TransactionEvent)
//{
//    if (TransactionEvent.GetEventType() == ETransactionObjectEventType::UndoRedo && Widget.IsValid())
//    {
//        Widget.Pin()->Tree->RequestTreeRefresh();
//    }
//}

void SLightTreeHierarchy::Construct(const FArguments& Args)
{
    



    SaveIcon = FSlateIconFinder::FindIcon("AssetEditor.SaveAsset");
    SaveAsIcon = FSlateIconFinder::FindIcon("AssetEditor.SaveAssetAs");
    LoadIcon = FSlateIconFinder::FindIcon("EnvQueryEditor.Profiler.LoadStats");

    FSlateFontInfo Font24(FCoreStyle::GetDefaultFont(), 20);
    _ASSERT(Args._ToolData);
    ToolData = Args._ToolData;
    DataUpdateDelegate = Args._DataUpdateDelegate;
    DataVerificationDelegate = Args._DataVerificationDelegate;
    SelectionChangedDelegate = Args._SelectionChangedDelegate;
    HeaderText = FText::FromString(Args._Name);

    ToolData->ItemExpansionChangedDelegate = FItemExpansionChangedDelegate::CreateLambda([this](UItemHandle* Item, bool bState)
        {
            Tree->SetItemExpansion(Item, bState);
        });

    ToolData->TreeStructureChangedDelegate = FOnTreeStructureChangedDelegate::CreateLambda([this]()
        {
            Tree->RequestTreeRefresh();
        });

    if (DataVerificationDelegate.IsBound())
        LightVerificationTimer = RegisterActiveTimer(Args._DataVerificationInterval, FWidgetActiveTimerDelegate::CreateRaw(this, &SLightTreeHierarchy::VerifyLights));
    else
        LightVerificationTimer.Reset();

    // SVerticalBox slots are by default dividing the space equally between each other
    // Because of this we need to expose the slot with the search bar in order to disable that for it

    SHorizontalBox::FSlot* SaveButtonSlot;
    SHorizontalBox::FSlot* SaveAsButtonSlot;
    SHorizontalBox::FSlot* LoadButtonSlot;

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
                //.OnSearch(this, &SLightTreeHierarchy::SearchBarSearch)
                .OnTextChanged(this, &SLightTreeHierarchy::SearchBarOnChanged)
                // Search bar for light
            ]
            +SVerticalBox::Slot()
            .VAlign(VAlign_Top)
            .AutoHeight()
            [
                SNew(STextBlock)
                .Text(HeaderText)
                .Font(Font24)
            ]
            +SVerticalBox::Slot()
                [
                    SNew(SHorizontalBox)
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Fill)
                    .VAlign(VAlign_Center)
                    [
                        SNew(STextBlock)
                        .Text(this, &SLightTreeHierarchy::GetPresetFilename)
                        .Font(FSlateFontInfo(FCoreStyle::GetDefaultFont(), 14))
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Right)
                    .VAlign(VAlign_Center)
                    .Expose(SaveButtonSlot)
                    [
                        SNew(SButton)
                        .ButtonColorAndOpacity(FSlateColor(FColor::Transparent))
                        .OnClicked_UObject(ToolData, &UToolData::SaveCallBack)
                        .RenderTransform(FSlateRenderTransform(0.9f))
                        .ToolTipText(FText::FromString("Save"))
                        [
                            SNew(SOverlay)
                            +SOverlay::Slot()
                            [
                                SNew(SImage)
                                .RenderOpacity(1.0f)
                                .Image(SaveIcon.GetIcon())
                            ]
                        ]
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Right)
                    .VAlign(VAlign_Center)
                    .Expose(SaveAsButtonSlot)
                    [
                        SNew(SButton)
                        .ButtonColorAndOpacity(FSlateColor(FColor::Transparent))
                        .OnClicked_UObject(ToolData, &UToolData::SaveAsCallback)
                        .RenderTransform(FSlateRenderTransform(0.9f))
                        .ToolTipText(FText::FromString("Save As"))
                        [
                            SNew(SImage)
                            .Image(SaveAsIcon.GetIcon())
                        ]
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Right)
                    .VAlign(VAlign_Center)
                    .Expose(LoadButtonSlot)
                    [
                        SNew(SButton)
                        .ButtonColorAndOpacity(FSlateColor(FColor::Transparent))
                        .OnClicked_UObject(ToolData, &UToolData::LoadCallBack)
                        .RenderTransform(FSlateRenderTransform(0.9f))
                        .ToolTipText(FText::FromString("Load"))
                        [
                            SNew(SImage)
                            .Image(LoadIcon.GetIcon())
                        ]
                    ]
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
                SAssignNew(Tree, STreeView<UItemHandle*>)
                .ItemHeight(24.0f)
                .TreeItemsSource(&ToolData->RootItems)
                .OnSelectionChanged(this, &SLightTreeHierarchy::SelectionCallback)
                .OnGenerateRow(this, &SLightTreeHierarchy::AddToTree)
                .OnGetChildren(this, &SLightTreeHierarchy::GetTreeItemChildren)
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

    SaveButtonSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    SaveAsButtonSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    LoadButtonSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    LightSearchBarSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    NewFolderButtonSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

}

void SLightTreeHierarchy::PreDestroy()
{
    if (LightVerificationTimer)
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
        auto NewItemHandle = ToolData->AddItem();
        NewItemHandle->Type = Type;
        NewItemHandle->Name = Actor->GetName();

        auto Item = Cast<UVirtualLight>(NewItemHandle->Item);

        switch (Type)
        {
        case SkyLight:
            Item->SkyLight = Cast<ASkyLight>(Actor);
            break;
        case SpotLight:
            Item->SpotLight = Cast<ASpotLight>(Actor);
            break;
        case DirectionalLight:
            Item->DirectionalLight = Cast<ADirectionalLight>(Actor);
            break;
        case PointLight:
            Item->PointLight = Cast<APointLight>(Actor);
            break;
        }
        DataUpdateDelegate.ExecuteIfBound(NewItemHandle);
        NewItemHandle->CheckNameAgainstSearchString(SearchString);

        ToolData->RootItems.Add(NewItemHandle);

        Tree->RequestTreeRefresh();
    }
}

void SLightTreeHierarchy::BeginTransaction()
{
    ToolData->Modify();
}

TSharedRef<ITableRow> SLightTreeHierarchy::AddToTree(UItemHandle* ItemPtr,
                                                     const TSharedRef<STableViewBase>& OwnerTable)
{
    SHorizontalBox::FSlot& CheckBoxSlot = SHorizontalBox::Slot();
    CheckBoxSlot.SizeParam.SizeRule = FSizeParam::SizeRule_Auto;


    if (!ItemPtr->Type == Folder || ItemPtr->Children.Num() > 0)
    {
        CheckBoxSlot
        [
            SAssignNew(ItemPtr->StateCheckbox, SCheckBox)
                .IsChecked_UObject(ItemPtr, &UItemHandle::IsLightEnabled)
                .OnCheckStateChanged_UObject(ItemPtr, &UItemHandle::OnCheck)
        ];
    }

    auto Row =
        SNew(STableRow<UItemHandle*>, OwnerTable)
        .Padding(2.0f)
        .OnDragDetected_UObject(ItemPtr, &UItemHandle::TreeDragDetected)
        .OnDrop_UObject(ItemPtr, &UItemHandle::TreeDropDetected)
        .Visibility_Lambda([ItemPtr]() {return ItemPtr->bMatchesSearchString ? EVisibility::Visible : EVisibility::Collapsed; })
        [
            SAssignNew(ItemPtr->TableRowBox, SBox)
        ];

    ItemPtr->GenerateTableRow();

    return Row;
}

void SLightTreeHierarchy::GetTreeItemChildren(UItemHandle* Item, TArray<UItemHandle*>& Children)
{
    Children.Append(Item->Children);
}

void SLightTreeHierarchy::SelectionCallback(UItemHandle* Item, ESelectInfo::Type SelectType)
{
    // Strangely, this callback is triggered when an redo is done that brings back a deleted group
    // When that happens, the tree widget is considered to have no selected items, which will incorrectly make the masterlight a nullptr
    // In these cases, Item is garbage and invalid, so by verifying the validity we ensure we don't set the masterlight to a nullptr incorrectly. 
    if (IsValid(Item))
    {

        auto Objects = Tree->GetSelectedItems();
        auto& SelectedItems = ToolData->SelectedItems;
        auto& LightsUnderSelection = ToolData->LightsUnderSelection;
        auto& SelectionMasterLight = ToolData->SelectionMasterLight;
        SelectedItems.Empty();

        for (auto Object : Objects)
        {
            SelectedItems.Add(Object);
        }

        if (SelectedItems.Num())
        {
            int Index;
            LightsUnderSelection.Empty();
            for (auto& Selected : SelectedItems)
            {
                Selected->GetLights(LightsUnderSelection);
            }
            if (LightsUnderSelection.Num())
            {
                if (SelectionMasterLight == nullptr || !LightsUnderSelection.Find(SelectionMasterLight, Index))
                {
                    //GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Yellow, "Selection");
                    SelectionMasterLight = LightsUnderSelection[0];
                }
            }
        }
        else
            SelectionMasterLight = nullptr;
        SelectionChangedDelegate.ExecuteIfBound();
    }
}

FReply SLightTreeHierarchy::AddFolderToTree()
{
    UItemHandle* NewFolder = ToolData->AddItem(true);
    NewFolder->Name = "New Group";
    NewFolder->CheckNameAgainstSearchString(SearchString);
    ToolData->RootItems.Add(NewFolder);

    Tree->RequestTreeRefresh();


    return FReply::Handled();
}

void SLightTreeHierarchy::TreeExpansionCallback(UItemHandle* Item, bool bExpanded)
{
    Item->bExpanded = bExpanded;
}

EActiveTimerReturnType SLightTreeHierarchy::VerifyLights(double, float)
{
    DataVerificationDelegate.Execute();

    return EActiveTimerReturnType::Continue;
}

void SLightTreeHierarchy::SearchBarOnChanged(const FText& NewString)
{
    SearchString = NewString.ToString();
    for (auto RootItem : ToolData->RootItems)
    {
        RootItem->CheckNameAgainstSearchString(SearchString);
    }

    //Tree->RequestTreeRefresh();
    Tree->RebuildList();
}

FText SLightTreeHierarchy::GetPresetFilename() const
{
    if (ToolData->ToolPresetPath.IsEmpty())
    {
        return FText::FromString("Not Saved");
    }
    FString Path, Name, Extension;
    FPaths::Split(ToolData->ToolPresetPath, Path, Name, Extension);
    return FText::FromString(Name);
}

