// Fill out your copyright notice in the Description page of Project Settings.


#include "TrackerBase.h"


void Print(FString Msg, FColor Color, float Time)
{
    if (GEngine)
    {
        GEngine->AddOnScreenDebugMessage(-1, Time, Color, Msg);
    }
}

bool operator!=(FTrackingInfo Left, FTrackingInfo Right)
{
    return Left.Track != Right.Track || Left.DeviceIndex != Right.DeviceIndex || Left.DeviceType != Right.DeviceType;
}

// Sets default values
ATrackerBase::ATrackerBase()
{
 	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

}

// Called when the game starts or when spawned
void ATrackerBase::BeginPlay()
{
	Super::BeginPlay();
    
}

void ATrackerBase::OnConstruction(const FTransform& Transform)
{
    if (TrackingInfo.Track && (!TrackingInfo.Calibrated || TrackingInfo != OldTrackingInfo))
    {
        Calibrate();
        OldTrackingInfo = TrackingInfo;
        Print("DevOrigin Rotation:  " + DeviceSpaceOrigin.GetRotation().Euler().ToString(), FColor::Orange, 60.0f);
        Print("DevOrigin Translation:  " + DeviceSpaceOrigin.GetTranslation().ToString(), FColor::Orange, 60.0f);

        auto DeltaRot = FQuat::FindBetween(DeviceSpaceOrigin.GetUnitAxis(EAxis::X), WorldSpaceOrigin.GetUnitAxis(EAxis::X));
        auto RotatedFWD = DeltaRot.RotateVector(DeviceSpaceOrigin.GetUnitAxis(EAxis::X));


        Print("Rotation:  " + DeltaRot.Euler().ToString(), FColor::Orange, 60.0f);
        Print("Rotated FWD:  " + RotatedFWD.ToString(), FColor::Orange, 60.0f);
        Print("DevOrigin FWD:  " + DeviceSpaceOrigin.GetUnitAxis(EAxis::X).ToString(), FColor::Orange, 60.0f);
        Print("WorldOrigin FWD:  " + WorldSpaceOrigin.GetUnitAxis(EAxis::X).ToString(), FColor::Orange, 60.0f);


    }
}

void ATrackerBase::UpdateVirtualPosition()
{
    //auto WorldSpaceTransform = DeviceSpaceTransform;// *WorldSpaceOrigin;
    auto WorldSpaceTransform = DeviceSpaceTransform * WorldSpaceOrigin;


    //WorldSpaceTransform.SetTranslation(DeviceSpaceTransform.GetTranslation() + WorldSpaceOrigin.GetTranslation());
    SetActorTransform(WorldSpaceTransform);
}

void ATrackerBase::Calibrate()
{
    DeviceSpaceOrigin = GetTrackerTransformRaw();
    WorldSpaceOrigin = GetActorTransform();
}

void ATrackerBase::UpdateTrackerTransform()
{
    auto DeviceSpaceOriginTranslation = DeviceSpaceOrigin.GetTranslation();

    auto Raw = GetTrackerTransformRaw();
    auto Translation = Raw.GetTranslation() - DeviceSpaceOriginTranslation;
    Print("Device Rotation:  " + Translation.ToString(), FColor::Red, 0.0f);
    Print("DSO Translation:  " + DeviceSpaceOriginTranslation.ToString(), FColor::Yellow, 0.0f);

    Translation = FQuat::MakeFromEuler(FVector(90.0f, 0.0f, 180.0f)).RotateVector(Translation);
    Print("Device Rotation:  " + Translation.ToString(), FColor::Yellow, 0.0f);
    //auto Translation = Raw.GetTranslation();
    //auto Rotation = DeviceSpaceOrigin.InverseTransformRotation(Raw.GetRotation());
    ////Rotation = WorldSpaceOrigin.TransformRotation(Rotation);
    //Rotation = Raw.GetRotation();

    auto DeltaRot = FQuat::FindBetween(DeviceSpaceOrigin.GetUnitAxis(EAxis::Z), WorldSpaceOrigin.GetUnitAxis(EAxis::X));
    auto DeltaRotTransformation = FTransform(DeltaRot);
    //auto RotatedFWD = DeltaRot.RotateVector(DeviceSpaceOrigin.GetUnitAxis(EAxis::X));
    //Rotation = Rotation * DeltaRot;

    //DeviceSpaceTransform.SetRotation(Rotation);

    ////Translation = Raw.GetTranslation() - DeviceSpaceOriginTranslation;
    //Translation = DeviceSpaceOrigin.InverseTransformPosition(Translation);

    ////Translation = DeltaRot.RotateVector(Translation);// (Translation.Y, Translation.X, Translation.Z);
    //DeviceSpaceTransform.SetTranslation(Translation * 1.0f);

    DeviceSpaceTransform.SetRotation(DeviceSpaceOrigin.InverseTransformRotation(Raw.GetRotation()));
    //DeviceSpaceTransform = DeviceSpaceTransform * DeltaRotTransformation;
    DeviceSpaceTransform.SetTranslation(Translation);

    Print("Device Rotation:  " + DeviceSpaceTransform.GetRotation().Euler().ToString(), FColor::Cyan, 0.0f);
    Print("Device Translation:  " + DeviceSpaceTransform.GetTranslation().ToString(), FColor::Cyan, 0.0f);

    Print("Raw Device Rotation:  " + Raw.GetRotation().Euler().ToString(), FColor::Blue, 0.0f);
    Print("Raw Device Translation:  " + Raw.GetTranslation().ToString(), FColor::Blue, 0.0f);

}

FTransform ATrackerBase::GetTrackerTransformRaw()
{
    FTransform Transform;

    if (!GEngine)
        return Transform; // Returns an identity matrix

    TArray<int32> Ids;
    USteamVRFunctionLibrary::GetValidTrackedDeviceIds(TrackingInfo.DeviceType, Ids);

    if (TrackingInfo.DeviceIndex >= Ids.Num())
    {
        GEngine->AddOnScreenDebugMessage(-1, -30.0f, FColor::Red, 
            "The device you are trying to track is not valid. Make sure its type is correct, and the index is not more than the devices that are being tracked");
    }
    else
    {
        auto Id = Ids[TrackingInfo.DeviceIndex];

        FVector Position;
        FRotator Rotation;

        USteamVRFunctionLibrary::GetTrackedDevicePositionAndOrientation(Id, Position, Rotation);

        Transform.SetTranslation(Position);
        Transform.SetRotation(Rotation.Quaternion());
    }
    return Transform;
}

// Called every frame
void ATrackerBase::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

    if (TrackingInfo.Track)
    {
        UpdateTrackerTransform();
        UpdateVirtualPosition();
    }

}

bool ATrackerBase::ShouldTickIfViewportsOnly() const
{
	// If the device is neither tracked nor is supposed to update its position in the viewport, default to the default value
	return (TrackingInfo.Track && TrackingInfo.TrackInViewport) || Super::ShouldTickIfViewportsOnly();
}

