#include "LightTreeHierarchy.h"
#include "Kismet/GameplayStatics.h"

#include "Engine/SkyLight.h"
#include "Engine/PointLight.h"
#include "Engine/SpotLight.h"
#include "Engine/DirectionalLight.h"

#include "Components/LightComponent.h"
#include "Components/SkyLightComponent.h"
#include "Components/PointLightComponent.h"
#include "Components/SpotLightComponent.h"

#include "Widgets/Layout/SScaleBox.h"

#include "Styling/SlateIconFinder.h"
#include "ClassIconFinder.h"

#include "Interfaces/IPluginManager.h"

#include "LightControlTool.h"
#include "Chaos/AABB.h"
#include "IO/DMXOutputPort.h"

#pragma region TreeItemStruct

void FLightDMXNotifyHook::NotifyPostChange(const FPropertyChangedEvent& PropertyChangedEvent, FProperty* PropertyThatChanged)
{
    //GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Orange, "Nihuyase");

    if (PropertiesRef->Conversion)
    {
        PropertiesRef->DataConverter = NewObject<ULightDataToDMXConversion>(PropertiesRef->Owner, PropertiesRef->Conversion);
    }

    if (PropertiesRef->DataConverter)
        PropertiesRef->DataConverter->StartingChannel = PropertiesRef->StartingChannel;
}

void ULightDataToDMXConversion::SetChannel(int32 InChannel, uint8 InValue)
{
    Channels.FindOrAdd(InChannel + StartingChannel - 1) = InValue;
}

ULightTreeItem::ULightTreeItem(SLightTreeHierarchy* InOwningWidget, FString InName, TArray<ULightTreeItem*> InChildren)
    : Name(InName)
    , Children(InChildren)
    , OwningWidget(InOwningWidget)
    , bInRename(false)
    , ActorPtr(nullptr)
    , bMatchesSearchString(true)
    , Intensity(0.0f)
    , Temperature(0.0f)
    , Hue(0.0f)
    , Saturation(0.0f)
    , Horizontal(0.0f)
    , Vertical(0.0f)
    , InnerAngle(0.0f)
    , OuterAngle(0.0f)
{
    SetFlags(GetFlags() | EObjectFlags::RF_Transactional);
    DMXProperties.Owner = this;
    DMXProperties.StartingChannel = 1;
}

ECheckBoxState ULightTreeItem::IsLightEnabled() const
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

void ULightTreeItem::OnCheck(ECheckBoxState NewState)
{
    bool B = false;
    if (NewState == ECheckBoxState::Checked)
        B = true;
    if (Type != Folder && !IsValid(ActorPtr))
        return;

    GEditor->BeginTransaction(FText::FromString(Name + " State change"));

    SetEnabled(B);

    GEditor->EndTransaction();
}

void ULightTreeItem::GenerateTableRow()
{
    auto IconType = Type;
    if (Type == Folder)
    {
        if (Children.Num())
        {
            IconType = Children[0]->Type; // This is 0 if there is a folder as the first child, which leads to out of bounds indexing
            for (size_t i = 1; i < Children.Num(); i++)
            {
                if (IconType != Children[i]->Type)
                {
                    IconType = Mixed;                    
                }
            }            
        }
        else
            IconType = Mixed;
    }
    CheckBoxStyle = OwningWidget->CoreToolPtr->MakeCheckboxStyleForType(IconType);

    CheckBoxStyle.CheckedPressedImage = CheckBoxStyle.UndeterminedImage;
    CheckBoxStyle.UncheckedPressedImage = CheckBoxStyle.UndeterminedImage;

    static auto Icon = *FSlateIconFinder::FindIconBrushForClass(APointLight::StaticClass());
    Icon.SetImageSize(FVector2D(16.0f, 16.0f));
    Icon.DrawAs = ESlateBrushDrawType::Box;
    Icon.Margin = FVector2D(0.0f, 0.0f);
    auto TintColor = FLinearColor(0.2f, 0.2f, 0.2f, 0.5f);
    Icon.TintColor = FSlateColor(TintColor);

    SHorizontalBox::FSlot* CheckBoxSlot;


    if (Type != Folder)
    {
        SHorizontalBox::FSlot* TextSlot;
        TableRowBox->SetContent(
        SNew(SHorizontalBox)
        +SHorizontalBox::Slot()
            .Expose(CheckBoxSlot) // On/Off toggle button 
            [
                SNew(SCheckBox)
                .IsChecked_UObject(this, &ULightTreeItem::IsLightEnabled)
                .OnCheckStateChanged_UObject(this, &ULightTreeItem::OnCheck)
                .Style(&CheckBoxStyle)
            ]
        + SHorizontalBox::Slot() // Name slot
            .Expose(TextSlot)
            .VAlign(VAlign_Center)
            [
                SAssignNew(RowNameBox, SBox) 
            ]
        +SHorizontalBox::Slot()
            .Padding(10.0f, 0.0f, 0.0f, 3.0f)
            .VAlign(VAlign_Bottom)
            [
                SNew(STextBlock)
                .Text(FText::FromString(Note))
            ]
        );

        TextSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    }
    else
    {
        SHorizontalBox::FSlot* FolderImageSlot;
        SHorizontalBox::FSlot* CloseButtonSlot;
        TableRowBox->SetContent(
            SNew(SHorizontalBox)
            +SHorizontalBox::Slot() // Name slot
            .VAlign(VAlign_Center)
            [
                SAssignNew(RowNameBox, SBox)
            ]
            +SHorizontalBox::Slot()
            .Expose(CloseButtonSlot)
            .HAlign(HAlign_Right)
            [
                SNew(SButton)
                .Text(FText::FromString("Delete"))
                .OnClicked_UObject(this, &ULightTreeItem::RemoveFromTree)
            ]
            +SHorizontalBox::Slot() // On/Off toggle button
            .Expose(CheckBoxSlot)
            .HAlign(HAlign_Right)
            [
                SAssignNew(StateCheckbox, SCheckBox)
                .IsChecked_UObject(this, &ULightTreeItem::IsLightEnabled)
                .OnCheckStateChanged_UObject(this, &ULightTreeItem::OnCheck)
                .Style(&CheckBoxStyle)
                .RenderTransform(FSlateRenderTransform(FScale2D(1.1f)))
            ]
            + SHorizontalBox::Slot()
                .Expose(FolderImageSlot)
                .HAlign(HAlign_Right)
                .Padding(3.0f, 0.0f, 3.0f, 0.0f)
                [
                    SNew(SButton)
                    .ButtonColorAndOpacity(FSlateColor(FColor::Transparent))
                    .OnClicked_Lambda([this]() {
                        bExpanded = !bExpanded;
                        ExpandInTree();
                        return FReply::Handled();
                    })
                    [
                        SNew(SImage) // Image overlay for the button
                        .Image_Lambda([this]() {return &(bExpanded ? OwningWidget->CoreToolPtr->GetIcon(FolderOpened) : OwningWidget->CoreToolPtr->GetIcon(FolderClosed)); })
                        .RenderTransform(FSlateRenderTransform(FScale2D(1.1f)))
                    ]                
            ]
        );
        //TableRowBox->SetRenderTransform(FSlateRenderTransform(FScale2D(1.2f)));
        UpdateFolderIcon();

        FolderImageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
        CloseButtonSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    }
    CheckBoxSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    auto Font = FSlateFontInfo(FCoreStyle::GetDefaultFont(), 10);
    if (Type == Folder) // Slightly larger font for group items
        Font.Size = 12;

    if (bInRename)
    {
        RowNameBox->SetContent(
            SNew(SEditableText)
            .Text(FText::FromString(Name))
            .Font(Font)
            .OnTextChanged_Lambda([this](FText Input)
                {
                    Name = Input.ToString();
                })
            .OnTextCommitted_UObject(this, &ULightTreeItem::EndRename));
            
    }
    else
    {
        RowNameBox->SetContent(
            SNew(STextBlock)
            .Text(FText::FromString(Name))
            .Font(Font)
            .ShadowColorAndOpacity(FLinearColor::Blue)
            .ShadowOffset(FIntPoint(-1, 1))
            .OnDoubleClicked_UObject(this, &ULightTreeItem::StartRename));
    }

    if (bMatchesSearchString)
        TableRowBox->SetVisibility(EVisibility::Visible);
    else
        TableRowBox->SetVisibility(EVisibility::Collapsed);
}

bool ULightTreeItem::VerifyDragDrop(ULightTreeItem* Dragged, ULightTreeItem* Destination)
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

bool ULightTreeItem::HasAsIndirectChild(ULightTreeItem* Item)
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

FReply ULightTreeItem::StartRename(const FGeometry&, const FPointerEvent&)
{
    bInRename = true;
    GenerateTableRow();
    return FReply::Handled();
}


void ULightTreeItem::EndRename(const FText& Text, ETextCommit::Type CommitType)
{
    if (ETextCommit::Type::OnEnter == CommitType)
    {
        Name = Text.ToString();
    }


    bInRename = false;
    GenerateTableRow();
}

TSharedPtr<FJsonValue> ULightTreeItem::SaveToJson()
{
    TSharedPtr<FJsonObject> Item = MakeShared<FJsonObject>();

    auto Enabled = IsLightEnabled();

    int ItemState = 0;// Unchecked
    if (Enabled == ECheckBoxState::Undetermined)
        ItemState = 1;
    else if (Enabled == ECheckBoxState::Checked)
        ItemState = 2;

    Item->SetStringField("Name", Name);
    Item->SetStringField("Note", Note);
    Item->SetNumberField("Type", Type);
    Item->SetBoolField("Expanded", bExpanded);
    if (Type != Folder)
    {
        Item->SetStringField("RelatedLightName", SkyLight->GetName());
        Item->SetNumberField("State", ItemState);
        Item->SetNumberField("Intensity", Intensity);
        Item->SetNumberField("Hue", Hue);
        Item->SetNumberField("Saturation", Saturation);
        Item->SetBoolField("UseTemperature", bUseTemperature);
        Item->SetNumberField("Temperature", Temperature);
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

void ULightTreeItem::PostTransacted(const FTransactionObjectEvent& TransactionEvent)
{
    UObject::PostTransacted(TransactionEvent);
    if (TransactionEvent.GetEventType() == ETransactionObjectEventType::UndoRedo)
    {
        if (Type != Folder)
            OwningWidget->CoreToolPtr->GetLightPropertyEditor().Pin()->UpdateSaturationGradient(OwningWidget->TransactionalVariables->SelectionMasterLight->Hue);
        else
           OwningWidget->Tree->RequestTreeRefresh();

        GenerateTableRow();
    }
}


ULightTreeItem::ELoadingResult ULightTreeItem::LoadFromJson(TSharedPtr<FJsonObject> JsonObject)
{
    Name = JsonObject->GetStringField("Name");
    Note = JsonObject->GetStringField("Note");
    Type = StaticCast<ETreeItemType>(JsonObject->GetNumberField("Type"));
    bExpanded = JsonObject->GetBoolField("Expanded");
    if (Type != Folder)
    {
        if (!GWorld) 
        {
            UE_LOG(LogTemp, Error, TEXT("There was an error with the engine. Try loading again. If the issue persists, restart the engine."));
            return EngineError;
        }
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
            UE_LOG(LogTemp, Error, TEXT("%s has invalid type: %n"), *Name, Type);
            return InvalidType;
        }
        TArray<AActor*> Actors;
        UGameplayStatics::GetAllActorsOfClass(GWorld, ClassToFetch, Actors);

        auto ActorPPtr = Actors.FindByPredicate([&LightName](AActor* Element){
                return Element && Element->GetName() == LightName;
            });

            
        if (!ActorPPtr)
        {
            UE_LOG(LogTemp, Error, TEXT("%s could not any lights in the scene named %s"), *Name, *LightName);
            return LightNotFound;
        }
        ActorPtr = *ActorPPtr;

        auto State = JsonObject->GetBoolField("State");

        SetEnabled(State);

        SetLightIntensity(JsonObject->GetNumberField("Intensity"));
        Hue = JsonObject->GetNumberField("Hue");
        Saturation = JsonObject->GetNumberField("Saturation");
        UpdateLightColor();
        SetUseTemperature(JsonObject->GetBoolField("UseTemperature"));
        SetTemperature(JsonObject->GetNumberField("Temperature"));

    }
    else
    {
        auto JsonChildren = JsonObject->GetArrayField("Children");

        auto ChildrenLoadingSuccess = Success;
        for (auto Child : JsonChildren)
        {
            const TSharedPtr<FJsonObject>* ChildObjectPtr;
            auto Success = Child->TryGetObject(ChildObjectPtr);
            auto ChildObject = *ChildObjectPtr;
            _ASSERT(Success);
            int ChildType = ChildObject->GetNumberField("Type");
            auto ChildItem = OwningWidget->AddTreeItem(ChildType == 0);

            ChildItem->Parent = this;

            auto ChildResult = ChildItem->LoadFromJson(ChildObject);
            if (ChildResult != ELoadingResult::Success)
            {
                if (ChildrenLoadingSuccess == ELoadingResult::Success)
                {
                    ChildrenLoadingSuccess = ChildResult;
                }
                else
                    ChildrenLoadingSuccess = ELoadingResult::MultipleErrors;
            }
                Children.Add(ChildItem);
        }
        return ChildrenLoadingSuccess;
    }


    return Success;
}

void ULightTreeItem::ExpandInTree()
{
    OwningWidget->Tree->SetItemExpansion(this, bExpanded);

    for (auto Child : Children)
    {
        Child->ExpandInTree();
    }
}

FReply ULightTreeItem::RemoveFromTree()
{
    GEditor->BeginTransaction(FText::FromString("Delete Light control folder"));
    BeginTransaction();
    if (Parent)
    {
        Parent->BeginTransaction();
        for (auto Child : Children)
        {
            Child->BeginTransaction();
            Child->Parent = Parent;
            Parent->Children.Add(Child);

        }
        Parent->Children.Remove(this);
    }
    else
    {
        OwningWidget->BeginTransaction();
        for (auto Child : Children)
        {
            Child->BeginTransaction();
            Child->Parent = nullptr;
            OwningWidget->TransactionalVariables->RootItems.Add(Child);
        }
        OwningWidget->TransactionalVariables->RootItems.Remove(this);
    }
    GEditor->EndTransaction();
    Children.Empty();
    OwningWidget->Tree->RequestTreeRefresh();

    return FReply::Handled();
}

void ULightTreeItem::FetchDataFromLight()
{
    _ASSERT(Type != Folder);

    FLinearColor RGB;

    Intensity = 0.0f;
    Saturation = 0.0f;
    Temperature = 0.0f;

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
    if (Saturation != 0.0f)
        Hue = HSV.R;

    if (Type == ETreeItemType::PointLight)
    {
        auto Comp = PointLight->PointLightComponent;
        Intensity = Comp->Intensity;       
    }    
    else if (Type == ETreeItemType::SpotLight)
    {
        auto Comp = SpotLight->SpotLightComponent;
        Intensity = Comp->Intensity;
    }

    if (Type != ETreeItemType::SkyLight)
    {
        auto LightPtr = Cast<ALight>(ActorPtr);
        auto LightComp = LightPtr->GetLightComponent();
        bUseTemperature = LightComp->bUseTemperature;
        Temperature = LightComp->Temperature;

        bCastShadows = LightComp->CastShadows;
    }
    else
    {
        bCastShadows = SkyLight->GetLightComponent()->CastShadows;
    }

    auto CurrentFwd = FQuat::MakeFromEuler(FVector(0.0f, Vertical, Horizontal)).GetForwardVector();
    auto ActorQuat = ActorPtr->GetTransform().GetRotation().GetNormalized();
    auto ActorFwd = ActorQuat.GetForwardVector();

    if (CurrentFwd.Equals(ActorFwd))
    {
        auto Euler = ActorQuat.Euler();
        Horizontal = Euler.Z;
        Vertical = Euler.Y;
    }
    

    if (Type == ETreeItemType::SpotLight)
    {
        InnerAngle = SpotLight->SpotLightComponent->InnerConeAngle;
        OuterAngle = SpotLight->SpotLightComponent->OuterConeAngle;
    }
    UpdateDMX();
}

void ULightTreeItem::UpdateLightColor()
{
    auto NewColor = FLinearColor::MakeFromHSV8(StaticCast<uint8>(Hue / 360.0f * 255.0f), StaticCast<uint8>(Saturation * 255.0f), 255);
    UpdateLightColor(NewColor);
}

void ULightTreeItem::UpdateLightColor(FLinearColor& Color)
{
    if (Type == ETreeItemType::SkyLight)
    {
        SkyLight->GetLightComponent()->SetLightColor(Color);
        SkyLight->GetLightComponent()->UpdateLightSpriteTexture();
    }
    else
    {
        auto LightPtr = Cast<ALight>(PointLight);
        LightPtr->SetLightColor(Color);
        LightPtr->GetLightComponent()->UpdateLightSpriteTexture();
    }
    UpdateDMX();
}

void ULightTreeItem::SetEnabled(bool bNewState)
{
    bIsEnabled = bNewState;
    BeginTransaction();
    switch (Type)
    {
    case ETreeItemType::SkyLight:
        SkyLight->GetLightComponent()->SetVisibility(bNewState);
        break;
    case ETreeItemType::SpotLight:
        SpotLight->GetLightComponent()->SetVisibility(bNewState);
        break;
    case ETreeItemType::DirectionalLight:
        DirectionalLight->GetLightComponent()->SetVisibility(bNewState);
        break;
    case ETreeItemType::PointLight:
        PointLight->GetLightComponent()->SetVisibility(bNewState);
        break;
    case ETreeItemType::Folder:
        for (auto& Child : Children)
        {
            Child->SetEnabled(bNewState);
        }
        break;
    }
}

void ULightTreeItem::SetLightIntensity(float NewValue)
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
            PointLightComp->SetIntensity(NewValue);            
            Intensity = NewValue;
        }
        else if (Type == ETreeItemType::SpotLight)
        {
            auto SpotLightComp = Cast<USpotLightComponent>(SpotLight->GetLightComponent());
            SpotLightComp->SetIntensityUnits(ELightUnits::Lumens);
            SpotLightComp->SetIntensity(NewValue);
            Intensity = NewValue;

        }
    }
    UpdateDMX();
}

void ULightTreeItem::SetUseTemperature(bool NewState)
{
    if (Type != ETreeItemType::SkyLight)
    {
        bUseTemperature = NewState;
        auto LightPtr = Cast<ALight>(ActorPtr);
        LightPtr->GetLightComponent()->SetUseTemperature(NewState);                
    }
    UpdateDMX();

}

void ULightTreeItem::SetTemperature(float NewValue)
{
    if (Type != ETreeItemType::SkyLight)
    {
        Temperature = NewValue;
        auto LightPtr = Cast<ALight>(ActorPtr);
        LightPtr->GetLightComponent()->SetTemperature(Temperature);
    }
    UpdateDMX();
}

void ULightTreeItem::SetCastShadows(bool bState)
{
    _ASSERT(Type != Folder);

    if (Type != ETreeItemType::SkyLight)
    {
        auto Light = Cast<ALight>(ActorPtr);
        Light->SetCastShadows(bState);
        bCastShadows = bState;        
    }
    else
    {
        auto SkyLightPtr = Cast<ASkyLight>(ActorPtr);
        SkyLightPtr->GetLightComponent()->SetCastShadows(bState);
        bCastShadows = bState;
    }
}

void ULightTreeItem::UpdateDMX()
{
    if (DMXProperties.bUseDmx && DMXProperties.OutputPort && DMXProperties.DataConverter)
    {
        DMXProperties.DataConverter->Channels.Empty();
        DMXProperties.DataConverter->Convert(this);

        //auto& Channels = DMXProperties.Channels;
        //auto Start = DMXProperties.StartingChannel;

        DMXProperties.OutputPort->SendDMX(1, DMXProperties.DataConverter->Channels);
    }
}

void ULightTreeItem::AddHorizontal(float Degrees)
{    
    auto Euler = ActorPtr->GetActorRotation().Euler();
    Euler.Z += Degrees;
    auto Rotator = FRotator::MakeFromEuler(Euler).GetNormalized();

    ActorPtr->SetActorRotation(Rotator);

    Horizontal += Degrees;
    Horizontal = FMath::Fmod(Horizontal + 180.0f, 360.0001f) - 180.0f;
    UpdateDMX();
}

void ULightTreeItem::AddVertical(float Degrees)
{
    auto ActorRot = ActorPtr->GetActorRotation().Quaternion();
    auto DeltaQuat = FVector::ForwardVector.RotateAngleAxis(Degrees, FVector::RightVector).Rotation().Quaternion();

    ActorPtr->SetActorRotation(ActorRot * DeltaQuat);

    Vertical += Degrees;
    Vertical = FMath::Fmod(Vertical + 180.0f, 360.0001f) - 180.0f;
    UpdateDMX();
}

void ULightTreeItem::SetInnerConeAngle(float NewValue)
{
    InnerAngle = NewValue;
    if (InnerAngle > OuterAngle)
    {
        SetOuterConeAngle(InnerAngle);
    }
    SpotLight->SetMobility(EComponentMobility::Movable);

    SpotLight->SpotLightComponent->SetInnerConeAngle(InnerAngle);
}


void ULightTreeItem::SetOuterConeAngle(float NewValue)
{
    SpotLight->SetMobility(EComponentMobility::Movable);
    if (bLockInnerAngleToOuterAngle)
    {
        auto Proportion = InnerAngle / OuterAngle;
        InnerAngle = Proportion * NewValue;
        SpotLight->SpotLightComponent->SetInnerConeAngle(InnerAngle);
    }


    OuterAngle = NewValue;
    OuterAngle = FMath::Max(OuterAngle, 1.0f); // Set the lower limit to 1.0 degree
    SpotLight->SpotLightComponent->SetOuterConeAngle(OuterAngle);

    if (InnerAngle > OuterAngle)
    {
        SetInnerConeAngle(OuterAngle);
    }
}

void ULightTreeItem::GetLights(TArray<ULightTreeItem*>& Array)
{
    if (Type == Folder)
    {
        for (auto& Child : Children)
            Child->GetLights(Array);
    }
    else
    {
        Array.Add(this);
    }
}

void ULightTreeItem::UpdateFolderIcon()
{
    if (Type != Folder)
        return;
    TArray<ULightTreeItem*> ChildLights;
    GetLights(ChildLights);

    auto IconType = Type;

    if (ChildLights.Num() > 0)
    {
        IconType = ChildLights[0]->Type;

        for (size_t i = 1; i < ChildLights.Num(); i++)
        {
            if (IconType != ChildLights[i]->Type)
            {
                IconType = Mixed;
                break;
            }
        }
    }
    else
        IconType = Mixed;

    

    if (Parent)
        Parent->UpdateFolderIcon();
}

bool ULightTreeItem::CheckNameAgainstSearchString(const FString& SearchString)
{
    bMatchesSearchString = false;
    if (SearchString.Len() == 0)
    {
        bMatchesSearchString = true;
    }
    else if (Name.Find(SearchString) != -1)
    {
        bMatchesSearchString = true;
    }

    for (auto ChildItem : Children)
    {
        bMatchesSearchString |= ChildItem->CheckNameAgainstSearchString(SearchString);
    }

    return bMatchesSearchString;
}

int ULightTreeItem::LightCount() const
{
    if (Type != Folder)
    {
        return 1;
    }
    auto LightCount = 0;

    for (auto Child : Children)
    {
        LightCount += Child->LightCount();
    }

    return LightCount;
}

void ULightTreeItem::BeginTransaction(bool bContinueRecursively)
{
    Modify();
    if (bContinueRecursively)
    {
        if (Type != Folder)
        {
            ActorPtr->Modify();
        }
        else
        {
            if (Parent)
                Parent->Modify();
        }
    }
}

FReply ULightTreeItem::TreeDragDetected(const FGeometry& Geometry, const FPointerEvent& MouseEvent)
{
    GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Green, Name + "Dragggg");


    TSharedRef<FTreeDropOperation> DragDropOp = MakeShared<FTreeDropOperation>();
    DragDropOp->DraggedItem = this;

    FReply Reply = FReply::Handled();

    Reply.BeginDragDrop(DragDropOp);

    return Reply;
}

FReply ULightTreeItem::TreeDropDetected(const FDragDropEvent& DragDropEvent)
{
    auto DragDrop = StaticCastSharedPtr<FTreeDropOperation>(DragDropEvent.GetOperation());
    auto Target = DragDrop->DraggedItem;
    auto Source = Target->Parent;
    ULightTreeItem* Destination = nullptr;

    if (!VerifyDragDrop(Target, this))
    {
        GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Green, "Drag drop cancelled");
        auto Reply = FReply::Handled();
        Reply.EndDragDrop();

        return FReply::Handled();
    }

    if (GEditor)
    {
        GEditor->BeginTransaction(FText::FromString("Light control tree drag and drop"));
    }

    // The source folder and the dragged item will always be affected, so always begin transacting them
    if (Source)
        Source->BeginTransaction();
    else
    {
        OwningWidget->BeginTransaction();
    }
    Target->BeginTransaction();

    if (Type == Folder)
    {
        
        Destination = this;

        Destination->BeginTransaction();
        

        if (Source)
            Source->Children.Remove(Target);
        else
            OwningWidget->TransactionalVariables->RootItems.Remove(Target);
        Destination->Children.Add(Target);
        Target->Parent = Destination;

        if (Source)
            Source->GenerateTableRow();
        Destination->GenerateTableRow();
        OwningWidget->Tree->SetItemExpansion(Destination, true);
    }
    else
    {

        Destination = OwningWidget->AddTreeItem(true);
        Destination->Name = Name + " Group";
        Destination->Parent = Parent;


        if (Parent)
            Parent->BeginTransaction();
        else
            OwningWidget->BeginTransaction();

        Destination->BeginTransaction();

        if (Parent)
        {
            Parent->Children.Remove(this);
            Parent->Children.Add(Destination);
        }
        else
        {
            OwningWidget->TransactionalVariables->RootItems.Remove(this);
            OwningWidget->TransactionalVariables->RootItems.Add(Destination);
        }

        if (Source)
            Source->Children.Remove(Target);
        else
            OwningWidget->TransactionalVariables->RootItems.Remove(Target);

        Destination->Children.Add(Target);
        Destination->Children.Add(this);

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

    GEditor->EndTransaction();
    Destination->UpdateFolderIcon();

    auto Reply = FReply::Handled();
    Reply.EndDragDrop();

    return FReply::Handled();
}

#pragma endregion

void UTreeTransactionalVariables::PostTransacted(const FTransactionObjectEvent& TransactionEvent)
{
    if (TransactionEvent.GetEventType() == ETransactionObjectEventType::UndoRedo && Widget.IsValid())
    {
        Widget.Pin()->Tree->RequestTreeRefresh();
    }
}

void SLightTreeHierarchy::Construct(const FArguments& Args)
{
    TransactionalVariables = NewObject<UTreeTransactionalVariables>();
    TransactionalVariables->Widget = SharedThis(this);
    TransactionalVariables->AddToRoot();


    LightVerificationTimer = RegisterActiveTimer(0.5f, FWidgetActiveTimerDelegate::CreateRaw(this, &SLightTreeHierarchy::VerifyLights));
    AutoSaveTimer = RegisterActiveTimer(30.0f, FWidgetActiveTimerDelegate::CreateRaw(this, &SLightTreeHierarchy::AutoSave)); // once every 5 minutes

    SaveIcon = FSlateIconFinder::FindIcon("AssetEditor.SaveAsset");
    SaveAsIcon = FSlateIconFinder::FindIcon("AssetEditor.SaveAssetAs");
    LoadIcon = FSlateIconFinder::FindIcon("EnvQueryEditor.Profiler.LoadStats");

    FSlateFontInfo Font24(FCoreStyle::GetDefaultFont(), 20);

    CoreToolPtr = Args._CoreToolPtr;


    UpdateLightList();

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
                .Text(FText::FromString("Scene Lights"))
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
                        .OnClicked(this, &SLightTreeHierarchy::SaveCallBack)
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
                        .OnClicked(this, &SLightTreeHierarchy::SaveAsCallback)
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
                        .OnClicked(this, &SLightTreeHierarchy::LoadCallBack)
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
                SAssignNew(Tree, STreeView<ULightTreeItem*>)
                .ItemHeight(24.0f)
                .TreeItemsSource(&TransactionalVariables->RootItems)
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

    SaveButtonSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    SaveAsButtonSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    LoadButtonSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    LightSearchBarSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    NewFolderButtonSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    LoadMetaData();

}

void SLightTreeHierarchy::PreDestroy()
{
    AutoSave(0.0, 0.0f);
    SaveMetaData();
    UnRegisterActiveTimer(LightVerificationTimer.ToSharedRef());
    UnRegisterActiveTimer(AutoSaveTimer.ToSharedRef());
    TransactionalVariables->RemoveFromRoot();
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

        TransactionalVariables->RootItems.Add(NewItem);

        Tree->RequestTreeRefresh();
    }
}

void SLightTreeHierarchy::BeginTransaction()
{
    TransactionalVariables->Modify();
}

TSharedRef<ITableRow> SLightTreeHierarchy::AddToTree(ULightTreeItem* ItemPtr,
                                                     const TSharedRef<STableViewBase>& OwnerTable)
{
    SHorizontalBox::FSlot& CheckBoxSlot = SHorizontalBox::Slot();
    CheckBoxSlot.SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    auto Item = Cast<ULightTreeItem>(ItemPtr);

    if (!Item->Type == Folder || Item->Children.Num() > 0)
    {
        CheckBoxSlot
        [
            SAssignNew(Item->StateCheckbox, SCheckBox)
                .IsChecked_UObject(Item, &ULightTreeItem::IsLightEnabled)
                .OnCheckStateChanged_UObject(Item, &ULightTreeItem::OnCheck)
        ];
    }

    auto Row =
        SNew(STableRow<ULightTreeItem*>, OwnerTable)
        .Padding(2.0f)
        .OnDragDetected_UObject(Item, &ULightTreeItem::TreeDragDetected)
        .OnDrop_UObject(Item, &ULightTreeItem::TreeDropDetected)
        .Visibility_Lambda([Item]() {return Item->bMatchesSearchString ? EVisibility::Visible : EVisibility::Collapsed; })
        [
            SAssignNew(Item->TableRowBox, SBox)
        ];

    Item->GenerateTableRow();

    return Row;
}

void SLightTreeHierarchy::GetChildren(ULightTreeItem* Item, TArray<ULightTreeItem*>& Children)
{
    Children.Append(Cast<ULightTreeItem>(Item)->Children);

}

void SLightTreeHierarchy::SelectionCallback(ULightTreeItem* Item, ESelectInfo::Type SelectType)
{
    auto Objects = Tree->GetSelectedItems();
    auto& SelectedItems = TransactionalVariables->SelectedItems;
    auto& LightsUnderSelection = TransactionalVariables->LightsUnderSelection;
    auto& SelectionMasterLight = TransactionalVariables->SelectionMasterLight;
    SelectedItems.Empty();

    for (auto Object : Objects)
    {
        SelectedItems.Add(Cast<ULightTreeItem>(Object));
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
    CoreToolPtr->OnTreeSelectionChanged();
}

FReply SLightTreeHierarchy::AddFolderToTree()
{
    ULightTreeItem* NewFolder = AddTreeItem(true);
    NewFolder->Name = "New Group";

    TransactionalVariables->RootItems.Add(NewFolder);

    Tree->RequestTreeRefresh();

    return FReply::Handled();
}

void SLightTreeHierarchy::TreeExpansionCallback(ULightTreeItem* Item, bool bExpanded)
{
    Cast<ULightTreeItem>(Item)->bExpanded = bExpanded;
}

ULightTreeItem* SLightTreeHierarchy::AddTreeItem(bool bIsFolder)
{
    auto Item = NewObject<ULightTreeItem>();
    Item->OwningWidget = this;
    Item->Parent = nullptr;

    TransactionalVariables->ListOfTreeItems.Add(Item);

    //TreeRootItems.Add(Item);
    if (bIsFolder)
    {
        Item->Type = Folder;
    }
    else // Do this so that only actual lights which might be deleted in the editor are checked for validity
        TransactionalVariables->ListOfLightItems.Add(Item);

    return Item;
}

EActiveTimerReturnType SLightTreeHierarchy::VerifyLights(double, float)
{
    if (bCurrentlyLoading)
        return EActiveTimerReturnType::Continue;
    GEngine->AddOnScreenDebugMessage(-1, 0.5f, FColor::Blue, "Cleaning invalid lights");
    TArray<ULightTreeItem*> ToRemove;
    for (auto Item : TransactionalVariables->ListOfLightItems)
    {
        if (!Item->ActorPtr || !IsValid(Item->SkyLight))
        {
            if (Item->Parent)
                Item->Parent->Children.Remove(Item);
            else
                TransactionalVariables->RootItems.Remove(Item);


            ToRemove.Add(Item);
        }
        else
        {
            Item->FetchDataFromLight();
        }
    }

    for (auto Item : ToRemove)
    {
        TransactionalVariables->ListOfTreeItems.Remove(Item);
        TransactionalVariables->ListOfLightItems.Remove(Item);
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
        ULightTreeItem* NewItem = AddTreeItem();
        NewItem->Type = ETreeItemType::PointLight;
        NewItem->Name = Light->GetName();
        NewItem->PointLight = Cast<APointLight>(Light);
        NewItem->FetchDataFromLight();

        TransactionalVariables->RootItems.Add(NewItem);

    }

    // Fetch Sky Lights
    UGameplayStatics::GetAllActorsOfClass(GWorld, ASkyLight::StaticClass(), Actors);
    for (auto Light : Actors)
    {
        ULightTreeItem* NewItem = AddTreeItem();
        NewItem->Type = ETreeItemType::SkyLight;
        NewItem->Name = Light->GetName();
        NewItem->SkyLight = Cast<ASkyLight>(Light);
        NewItem->FetchDataFromLight();

        TransactionalVariables->RootItems.Add(NewItem);
    }

    // Fetch Directional Lights
    UGameplayStatics::GetAllActorsOfClass(GWorld, ADirectionalLight::StaticClass(), Actors);
    for (auto Light : Actors)
    {
        ULightTreeItem* NewItem = AddTreeItem();
        NewItem->Type = ETreeItemType::DirectionalLight;
        NewItem->Name = Light->GetName();
        NewItem->DirectionalLight = Cast<ADirectionalLight>(Light);
        NewItem->FetchDataFromLight();

        TransactionalVariables->RootItems.Add(NewItem);
    }

    // Fetch Spot Lights
    UGameplayStatics::GetAllActorsOfClass(GWorld, ASpotLight::StaticClass(), Actors);
    for (auto Light : Actors)
    {
        ULightTreeItem* NewItem = AddTreeItem();
        NewItem->Type = ETreeItemType::SpotLight;
        NewItem->Name = Light->GetName();
        NewItem->SpotLight = Cast<ASpotLight>(Light);
        NewItem->FetchDataFromLight();

        TransactionalVariables->RootItems.Add(NewItem);
    }
}

void SLightTreeHierarchy::SearchBarOnChanged(const FText& NewString)
{
    for (auto RootItem : TransactionalVariables->RootItems)
    {
        Cast<ULightTreeItem>(RootItem)->CheckNameAgainstSearchString(NewString.ToString());
    }

    //Tree->RequestTreeRefresh();
    Tree->RebuildList();
}

EActiveTimerReturnType SLightTreeHierarchy::AutoSave(double, float)
{
    UE_LOG(LogTemp, Display, TEXT("Autosaving light control tool state."));

    if (ToolPresetPath.IsEmpty())
    {
        auto ThisPlugin = IPluginManager::Get().FindPlugin("CradleLightControl");
        auto Content = ThisPlugin->GetContentDir();

        SaveStateToJson(Content + "/LightsAutoSave.json", false);
    }
    else
        SaveStateToJson(ToolPresetPath, false);

    SaveMetaData();

    return EActiveTimerReturnType::Continue;
}

FReply SLightTreeHierarchy::SaveCallBack()
{
    if (ToolPresetPath.IsEmpty())
    {
        return SaveAsCallback();
    }
    else
        SaveStateToJson(ToolPresetPath);

    return FReply::Handled();
}


FReply SLightTreeHierarchy::SaveAsCallback()
{
    auto ThisPlugin = IPluginManager::Get().FindPlugin("CradleLightControl");
    auto Content = ThisPlugin->GetContentDir();

    TArray<FString> Filenames;
    if (CoreToolPtr->SaveFileDialog("Select file to save tool state to", Content, 0 /*Single file*/, "Data Table JSON (*.json)|*.json", Filenames))
    {
        auto TargetFile = Filenames[0];
        SaveStateToJson(TargetFile);
    }
    return FReply::Handled();
}

void SLightTreeHierarchy::SaveStateToJson(FString Path, bool bUpdatePresetPath)
{
    TArray<TSharedPtr<FJsonValue>> TreeItemsJSON;

    if (IsValid(TransactionalVariables))
    {
        for (auto TreeItem : TransactionalVariables->RootItems)
        {
            TreeItemsJSON.Add(TreeItem->SaveToJson());
        }
    }
    TSharedPtr<FJsonObject> RootObject = MakeShared<FJsonObject>();

    RootObject->SetArrayField("TreeElements", TreeItemsJSON);

    FString Output;
    TSharedRef<TJsonWriter<>> Writer = TJsonWriterFactory<>::Create(&Output);
    FJsonSerializer::Serialize(RootObject.ToSharedRef(), Writer);

    FFileHelper::SaveStringToFile(Output, *Path);
    if (bUpdatePresetPath)
        ToolPresetPath = Path;
}

FReply SLightTreeHierarchy::LoadCallBack()
{
    auto ThisPlugin = IPluginManager::Get().FindPlugin("CradleLightControl");
    auto Content = ThisPlugin->GetContentDir();
    TArray<FString> Filenames;
    if (CoreToolPtr->OpenFileDialog("Select file to restore tool state from", Content, 0 /*Single file*/, "Data Table JSON (*.json)|*.json", Filenames))
    {
        auto TargetFile = Filenames[0];
        LoadStateFromJSON(TargetFile);
    }
    return FReply::Handled();
}

void SLightTreeHierarchy::LoadStateFromJSON(FString Path, bool bUpdatePresetPath)
{
    bCurrentlyLoading = true;

    FString Input;
    if (CoreToolPtr) CoreToolPtr->ClearSelection();
    if (FFileHelper::LoadFileToString(Input, *Path))
    {
        if (bUpdatePresetPath)
            ToolPresetPath = Path;
        UE_LOG(LogTemp, Display, TEXT("Beginning light control tool state loading from %s"), *Path);
        TransactionalVariables->RootItems.Empty();
        TransactionalVariables->ListOfTreeItems.Empty();
        TransactionalVariables->ListOfLightItems.Empty();
        TSharedPtr<FJsonObject> JsonRoot;
        TSharedRef<TJsonReader<>> JsonReader = TJsonReaderFactory<>::Create(Input);
        FJsonSerializer::Deserialize(JsonReader, JsonRoot);

        auto LoadingResult = ULightTreeItem::ELoadingResult::Success;
        for (auto TreeElement : JsonRoot->GetArrayField("TreeElements"))
        {
            const TSharedPtr<FJsonObject>* TreeElementObjectPtr;
            auto Success = TreeElement->TryGetObject(TreeElementObjectPtr);
            auto TreeElementObject = *TreeElementObjectPtr;
            _ASSERT(Success);
            int Type = TreeElementObject->GetNumberField("Type");
            auto Item = AddTreeItem(Type == 0); // If Type is 0, this element is a folder, so we add it as a folder
            auto Res = Item->LoadFromJson(TreeElementObject);

            if (Res != ULightTreeItem::ELoadingResult::Success)
            {
                if (LoadingResult == ULightTreeItem::ELoadingResult::Success)
                {
                    LoadingResult = Res;
                }
                else LoadingResult = ULightTreeItem::ELoadingResult::MultipleErrors;
            }

            TransactionalVariables->RootItems.Add(Item);
        }
        Tree->RequestTreeRefresh();

        for (auto TreeItem : TransactionalVariables->RootItems)
        {
            Cast<ULightTreeItem>(TreeItem)->ExpandInTree();
        }

        if (LoadingResult == ULightTreeItem::ELoadingResult::Success)
        {
            UE_LOG(LogTemp, Display, TEXT("Light control state loaded successfully"));
        }
        else
        {
            FString ErrorMessage = "";

            switch (LoadingResult)
            {
            case ULightTreeItem::LightNotFound:
                ErrorMessage = "At least one light could not be found. Please ensure all lights exist and haven't been renamed since the save.";
                break;
            case ULightTreeItem::EngineError:
                ErrorMessage = "There was an error with the engine. Please try loading again. If the error persists, restart the engine.";
                break;
            case ULightTreeItem::InvalidType:
                ErrorMessage = "The item type that was tried to be loaded was not valid. Please ensure that the item type in the .json file is between 0 and 4.";
                break;
            case ULightTreeItem::MultipleErrors:
                ErrorMessage = "Multiple errors occurred. See output log for more details.";
                break;
            }

            UE_LOG(LogTemp, Display, TEXT("Light control state could not load with following message: %s"), *ErrorMessage);

            FNotificationInfo NotificationInfo(FText::FromString(FString::Printf(TEXT("Light control tool state could not be loaded. Please check the output log."))));

            NotificationInfo.ExpireDuration = 300.0f;
            NotificationInfo.bUseSuccessFailIcons = false;

            FSlateNotificationManager::Get().AddNotification(NotificationInfo);
        }
    }
    else
    {
        UE_LOG(LogTemp, Error, TEXT("Could not open file %s"), *Path);
        ToolPresetPath = "";
    }

    
    bCurrentlyLoading = false;
}

FText SLightTreeHierarchy::GetPresetFilename() const
{
    if (ToolPresetPath.IsEmpty())
    {
        return FText::FromString("Not Saved");
    }
    FString Path, Name, Extension;
    FPaths::Split(ToolPresetPath, Path, Name, Extension);
    return FText::FromString(Name);
}

void SLightTreeHierarchy::SaveMetaData()
{
    UE_LOG(LogTemp, Display, TEXT("Saving light control meta data."));
    auto ThisPlugin = IPluginManager::Get().FindPlugin("CradleLightControl");
    auto Content = ThisPlugin->GetContentDir();

    TSharedPtr<FJsonObject> RootObject = MakeShared<FJsonObject>();

    RootObject->SetStringField("LastUsedPreset", ToolPresetPath);

    FString Output;
    TSharedRef<TJsonWriter<>> Writer = TJsonWriterFactory<>::Create(&Output);
    FJsonSerializer::Serialize(RootObject.ToSharedRef(), Writer);

    FFileHelper::SaveStringToFile(Output, *(Content + "/LightControlMeta.json"));
}

void SLightTreeHierarchy::LoadMetaData()
{
    auto ThisPlugin = IPluginManager::Get().FindPlugin("CradleLightControl");
    auto Content = ThisPlugin->GetContentDir();
    FString Input;
    if (FFileHelper::LoadFileToString(Input, *(Content + "/LightControlMeta.json")))
    {
        UE_LOG(LogTemp, Display, TEXT("Loading light control meta data."));
        TSharedPtr<FJsonObject> JsonRoot;
        TSharedRef<TJsonReader<>> JsonReader = TJsonReaderFactory<>::Create(Input);
        FJsonSerializer::Deserialize(JsonReader, JsonRoot);

        ToolPresetPath = JsonRoot->GetStringField("LastUsedPreset");
        if (!ToolPresetPath.IsEmpty())
        {
            LoadStateFromJSON(ToolPresetPath, false);            
        }
        else
        {
            LoadStateFromJSON(Content + "/LightsAutoSave.json", false);
        }
    }
    else
        UE_LOG(LogTemp, Error, TEXT("Failed to load light control meta data."));

}
