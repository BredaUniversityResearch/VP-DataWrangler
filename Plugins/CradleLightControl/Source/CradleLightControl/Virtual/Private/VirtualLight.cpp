#include "VirtualLight.h"

#include "ItemHandle.h"

#include "Engine/Light.h"
#include "Engine/SpotLight.h"
#include "Engine/SkyLight.h"
#include "Engine/PointLight.h"
#include "Engine/DirectionalLight.h"

#include "Components/PointLightComponent.h"
#include "Components/SpotLightComponent.h"
#include "Components/SkyLightComponent.h"
#include "Kismet/GameplayStatics.h"

void UVirtualLight::SetEnabled(bool bNewState)
{
    UBaseLight::SetEnabled(bNewState);

    BeginTransaction();
    switch (Handle->Type)
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
    }
}

void UVirtualLight::SetLightIntensity(float NormalizedValue)
{

    if (Handle->Type == ETreeItemType::SkyLight)
    {
        auto LightComp = SkyLight->GetLightComponent();
        LightComp->Intensity = NormalizedValue;
    }
    else
    {
        auto ValLumen = NormalizedValue * 2010.619f;
        if (Handle->Type == ETreeItemType::PointLight)
        {
            auto PointLightComp = Cast<UPointLightComponent>(PointLight->GetLightComponent());
            PointLightComp->SetIntensityUnits(ELightUnits::Lumens);
            PointLightComp->SetIntensity(ValLumen);
            Intensity = ValLumen;
        }
        else if (Handle->Type == ETreeItemType::SpotLight)
        {
            auto SpotLightComp = Cast<USpotLightComponent>(SpotLight->GetLightComponent());
            SpotLightComp->SetIntensityUnits(ELightUnits::Lumens);
            SpotLightComp->SetIntensity(ValLumen);
            Intensity = ValLumen;

        }
    }
}

void UVirtualLight::SetHue(float NewValue)
{
    Super::SetHue(NewValue);
    if (Handle->Type == ETreeItemType::SkyLight)
        SkyLight->GetLightComponent()->SetLightColor(GetRGBColor());
    else
        Cast<ALight>(ActorPtr)->SetLightColor(GetRGBColor());
}

void UVirtualLight::SetSaturation(float NewValue)
{
    Super::SetSaturation(NewValue);

    if (Handle->Type == ETreeItemType::SkyLight)
        SkyLight->GetLightComponent()->SetLightColor(GetRGBColor());
    else
        Cast<ALight>(ActorPtr)->SetLightColor(GetRGBColor());
}

void UVirtualLight::SetUseTemperature(bool NewState)
{
    if (Handle->Type != ETreeItemType::SkyLight)
    {
        UBaseLight::SetUseTemperature(NewState);
        auto LightPtr = Cast<ALight>(ActorPtr);
        LightPtr->GetLightComponent()->SetUseTemperature(NewState);
    }
}

void UVirtualLight::SetTemperature(float NormalizedValue)
{
    if (Handle->Type != ETreeItemType::SkyLight)
    {
        Temperature = NormalizedValue * (12000.0f - 1700.0f) + 1700.0f;
        auto LightPtr = Cast<ALight>(ActorPtr);
        LightPtr->GetLightComponent()->SetTemperature(Temperature);
    }

}

void UVirtualLight::SetCastShadows(bool bState)
{
    if (Handle->Type != ETreeItemType::SkyLight)
    {
        auto Light = Cast<ALight>(ActorPtr);
        Light->SetCastShadows(bState);
        bCastShadows = bState;
    }
    else
    {
        SkyLight->GetLightComponent()->SetCastShadows(bState);
        bCastShadows = bState;
    }
}

FPlatformTypes::uint8 UVirtualLight::LoadFromJson(TSharedPtr<FJsonObject> JsonObject)
{


    auto LightName = JsonObject->GetStringField("RelatedLightName");

    UClass* ClassToFetch = AActor::StaticClass();

    switch (Handle->Type)
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
        UE_LOG(LogTemp, Error, TEXT("%s has invalid type: %n"), *Handle->Name, Handle->Type);
        return UItemHandle::ELoadingResult::InvalidType;
    }
    TArray<AActor*> Actors;
    UGameplayStatics::GetAllActorsOfClass(GWorld, ClassToFetch, Actors);

    auto ActorPPtr = Actors.FindByPredicate([&LightName](AActor* Element) {
        return Element && Element->GetName() == LightName;
        });


    if (!ActorPPtr)
    {
        UE_LOG(LogTemp, Error, TEXT("%s could not any lights in the scene named %s"), *Handle->Name, *LightName);
        return UItemHandle::ELoadingResult::LightNotFound;
    }
    ActorPtr = *ActorPPtr;
    // Load the default properties
    auto Res = UBaseLight::LoadFromJson(JsonObject);

    return Res;
}

void UVirtualLight::BeginTransaction()
{
    Super::BeginTransaction();

    ActorPtr->Modify();
}

void UVirtualLight::AddHorizontal(float Degrees)
{
    auto Euler = ActorPtr->GetActorRotation().Euler();
    Euler.Z += Degrees;
    auto Rotator = FRotator::MakeFromEuler(Euler).GetNormalized();

    ActorPtr->SetActorRotation(Rotator);

    Horizontal += Degrees;
    Horizontal = FMath::Fmod(Horizontal + 180.0f, 360.0001f) - 180.0f;
}

void UVirtualLight::AddVertical(float Degrees)
{
    auto ActorRot = ActorPtr->GetActorRotation().Quaternion();
    auto DeltaQuat = FVector::ForwardVector.RotateAngleAxis(Degrees, FVector::RightVector).Rotation().Quaternion();

    ActorPtr->SetActorRotation(ActorRot * DeltaQuat);

    Vertical += Degrees;
    Vertical = FMath::Fmod(Vertical + 180.0f, 360.0001f) - 180.0f;
}

void UVirtualLight::SetInnerConeAngle(float NewValue)
{
    _ASSERT(Handle->Type == ETreeItemType::SpotLight);
    InnerAngle = NewValue;
    if (InnerAngle > OuterAngle)
    {
        SetOuterConeAngle(InnerAngle);
    }
    SpotLight->SetMobility(EComponentMobility::Movable);

    SpotLight->SpotLightComponent->SetInnerConeAngle(InnerAngle);
}


void UVirtualLight::SetOuterConeAngle(float NewValue)
{
    _ASSERT(Handle->Type == ETreeItemType::SpotLight);
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

bool UVirtualLight::GetCastShadows() const
{
    return bCastShadows;
}

TSharedPtr<FJsonObject> UVirtualLight::SaveAsJson() const
{
    auto JsonObject = Super::SaveAsJson();
    JsonObject->SetStringField("RelatedLightName", ActorPtr->GetName());

    return JsonObject;
}
