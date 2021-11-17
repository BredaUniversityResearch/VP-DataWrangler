// Fill out your copyright notice in the Description page of Project Settings.


#include "TrackerComponent.h"

#include "SteamVRFunctionLibrary.h"


// Sets default values for this component's properties
UTrackerComponent::UTrackerComponent()
{
	// Set this component to be initialized when the game starts, and to be ticked every frame.  You can turn these features
	// off to improve performance if you don't need them.
	PrimaryComponentTick.bCanEverTick = true;
	bTickInEditor = true;
	bAutoActivate = true;

	// ...
}


// Called when the game starts
void UTrackerComponent::BeginPlay()
{
	Super::BeginPlay();
	
	// ...
	
}


// Called every frame
void UTrackerComponent::TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction)
{
	if (bTrack)
	{
		auto CurrentTransform = GetCurrentTrackerTransform();
		auto Loc = CurrentTransform.GetLocation();

		auto Distance = FVector::Distance(Loc, TrackerLocation);

			LocationBuffer.Add(Loc);
		if (Distance < 0.1f)
		{
			while (LocationBuffer.Num() > 30)
			{
				LocationBuffer.RemoveAt(0);
			}
		}
		else
		{
			while (LocationBuffer.Num() > 2)
			{
				//GEditor->AddOnScreenDebugMessage(-1, 0.9f, FColor::Cyan, FString::Printf(TEXT("%f"), FVector::Distance(Loc, TrackerLocation)));

				LocationBuffer.RemoveAt(0);
			}
		}

		FVector Sum = FVector::ZeroVector;
		for (auto V : LocationBuffer)
		{
			Sum += V;
		}

		Sum /= LocationBuffer.Num();
		//GEditor->AddOnScreenDebugMessage(-1, 0.9f, FColor::Cyan, FString::Printf(TEXT("%f"), FVector::Distance(Sum, TrackerLocation)));


		TrackerLocation = Sum;

		TrackerRotation = CurrentTransform.GetRotation().Rotator();
		//TrackerLocation = Loc;
		UpdateVirtualTransform();
	}

	TArray<USceneComponent*> ChildComponents;

	GetChildrenComponents(false, ChildComponents);
	if (ChildComponents.Num())
	{
		GEditor->AddOnScreenDebugMessage(-1, 0.1f, FColor::Cyan, ChildComponents[0]->GetName());
		if (GetOwner())
			GEditor->AddOnScreenDebugMessage(-1, 0.1f, FColor::Black, GetOwner()->GetName());
		
	}
	Super::TickComponent(DeltaTime, TickType, ThisTickFunction);

	// ...
}

void UTrackerComponent::UpdateVirtualTransform()
{
	// There is no way to guarantee that the tracker's position and rotation are all zeroed out
	// Because of that during calibration the tracker's position and rotation are recorded and consecutive movements of the tracker are applied in regards to those.

	// Use the inverse of the physical origin transform to make the physical calibration point the origin for the tracker's location and orientation


	auto Location = TrackerLocation - DeviceSpaceOrigin.GetTranslation();
	auto Orientation = TrackerRotation.Quaternion() * DeviceSpaceOrigin.GetRotation().Inverse();
	//auto Orientation = PhysicalOrigin.InverseTransformRotation(TrackerRotation.Quaternion());
	//Orientation = TrackerRotation.Quaternion();
	// The location and orientation are now representative only of the movement and rotation that was done after the calibration.

	// To account for potential differences in device coordinate space based on the room it is used in,
	// a singleton has been introduced which stores a "Room Correction Transform", which is used to counteract these differences
	// Generally there shouldn't be any problems with this, unless the singleton for the project was just changed
	FTransform RoomTransform = FTransform::Identity;
	//auto VPSingleton = Cast<UVPSingleton>(GEngine->GameSingleton);

	//if (VPSingleton)
	//{
	//	// TODO: The room transform needs to be exposed via a UI element at a later date, rather than having it hard-coded here
	//	VPSingleton->RoomCorrection.SetRotation(FQuat::MakeFromEuler(FVector(0.0f, 0.0f, 0.0f)));
	//	RoomTransform = VPSingleton->RoomCorrection.Inverse();
	//}

	Location = RoomTransform.TransformPosition(Location);
	Orientation = RoomTransform.TransformRotation(Orientation);

	// Simple fix to increase the effect tracker movement has on the tracker
	// Probably not realistic for virtual production, so default to 1.0f
	Location = Location * MovementMultiplier;

	// The location and orientation are now facing at the same direction as a physical camera would, and is moved according to the tracker's movement
	// They are still located at the world origin however.


	// All that is left is applying the virtual origin transform to move from the world origin to the desired location in the level
	Location = WorldSpaceOrigin.TransformPosition(Location);
	Orientation = WorldSpaceOrigin.TransformRotation(Orientation);

	// Set the location and rotation of the actor after all the transformations
	GetOwner()->SetActorLocationAndRotation(Location, Orientation);

}

FTransform UTrackerComponent::GetCurrentTrackerTransform()
{
	if (!GEngine)
	{
		return FTransform::Identity;
	}

	TArray<int32> DeviceIds;
	USteamVRFunctionLibrary::GetValidTrackedDeviceIds(DeviceType, DeviceIds);

	FVector Location = FVector::ZeroVector;
	FRotator Rotation = FRotator::ZeroRotator;

	if (!DeviceIds.IsValidIndex(DeviceId))
	{
		GEngine->AddOnScreenDebugMessage(-1, 0.0f, FColor::Red, FString::Printf(TEXT("%s/%s could not find tracking device with Id %d"), *GetOwner()->GetName(), *GetName(), DeviceId));
	}
	else
	{
		USteamVRFunctionLibrary::GetTrackedDevicePositionAndOrientation(DeviceIds[DeviceId], Location, Rotation);
	}

	return FTransform(Rotation.Quaternion(), Location);
}

void UTrackerComponent::Calibrate()
{
	auto CurrentTransform = GetCurrentTrackerTransform();
	TrackerLocation = CurrentTransform.GetLocation();
	TrackerRotation = CurrentTransform.GetRotation().Rotator();

	DeviceSpaceOrigin = FTransform(TrackerRotation.Quaternion(), TrackerLocation);

	if (IsValid(TrackerCalibrationPoint))
	{
		WorldSpaceOrigin = TrackerCalibrationPoint->GetActorTransform();
	}
	else
	{
		GEngine->AddOnScreenDebugMessage(-1, 0.0f, FColor::Red, FString::Printf(TEXT("%s/%s was not assigned a valid calibration point, resorting to using the world origin as tracker origin."), *GetOwner()->GetName(), *GetName()));
	}

	auto OwnerActor = GetOwner();

	OwnerActor->SetActorLocation(WorldSpaceOrigin.GetLocation());
	OwnerActor->SetActorRotation(WorldSpaceOrigin.GetRotation());
}

bool UTrackerComponent::MovementCheckDistance(float Threshold)
{
	if (LocationBuffer.Num() < 2)
	{
		return false;
	}
	auto Distance = FVector::Distance(LocationBuffer.Last(), LocationBuffer.Last(1));
	return Distance > Threshold;
}

