// Fill out your copyright notice in the Description page of Project Settings.


#include "VPCamera.h"

#include "SteamVRFunctionLibrary.h"

#include "VPSingleton.h"

// Lazy function so that I can quickly print out a message in unreal while debugging
void EzPrint(FString String, FColor Color = FColor::Cyan, float Duration = 3.0f)
{
	if (GEngine)
	{
		GEngine->AddOnScreenDebugMessage(-1, Duration, Color, String);
	}
}

// Sets default values
AVPCamera::AVPCamera()
{
	// Set this pawn to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

	RootComponent = CreateDefaultSubobject<USceneComponent>("Scene Root");

	SpringArmComponent = CreateDefaultSubobject<USpringArmComponent>("Attachment Spring Arm");

	FAttachmentTransformRules AttachmentRules(EAttachmentRule::SnapToTarget, false);

	SpringArmComponent->AttachToComponent(RootComponent, AttachmentRules);
	SpringArmComponent->bDrawDebugLagMarkers = true;
	SpringArmComponent->CameraLagSpeed = 5.0f;
	SpringArmComponent->CameraLagMaxDistance = 5.0f;
	SpringArmComponent->bEnableCameraLag = true;
	SpringArmComponent->TargetArmLength = 0.0f;


}

// Called when the game starts or when spawned
void AVPCamera::BeginPlay()
{
	Super::BeginPlay();
}

void AVPCamera::OnConstruction(const FTransform& Transform)
{
	if (CalibrateOnPropertyChange)
	{
		Calibrate();
	}

	SpringArmComponent->SocketOffset = OffsetFromObjectToTracker;
}

void AVPCamera::UpdateTrackerState()
{
	// Ensure that the engine has been fully created before trying to do anything with the trackers
	// This is needed because the actor is by default allowed to tick outside of PIE,
	// so I've had it tick before the engine has finished instantiating
	if (!GEngine)
	{
		return;
	}

	// This is going to use the device transform of the last device it finds, so this needs to be fixed.
	// TODO: Add variables which allow control over which device is used
	TArray<int32_t> DeviceIds;
	USteamVRFunctionLibrary::GetValidTrackedDeviceIds(ESteamVRTrackedDeviceType::Other, DeviceIds);

	if (!DeviceIds.Num())
	{
		if (GEngine)
			GEngine->AddOnScreenDebugMessage(-1, 0.0, FColor::Red, FString::Printf(TEXT("No trackers found")));
	}
	else
	{
		if (GEngine)
			GEngine->AddOnScreenDebugMessage(-1, 0.0, FColor::Cyan, FString::Printf(TEXT("Number of SteamVR device Ids: %i"), DeviceIds.Num()));

	}


	for (auto& Id : DeviceIds)
	{
		USteamVRFunctionLibrary::GetTrackedDevicePositionAndOrientation(Id, TrackerLocation, TrackerRotation);

		if (GEngine)
			GEngine->AddOnScreenDebugMessage(-1, 0.0f, FColor::Blue, "Device has position:  " + TrackerLocation.ToString());

		if (GEngine)
			GEngine->AddOnScreenDebugMessage(-1, 0.0f, FColor::Green, "Device has rotation:  " + TrackerRotation.Euler().ToString());

	}
}

void AVPCamera::UpdateVirtualTransform()
{





	// There is no way to guarantee that the tracker's position and rotation are all zeroed out
	// Because of that during calibration the tracker's position and rotation are recorded and consecutive movements of the tracker are applied in regards to those.

	// Use the inverse of the physical origin transform to make the physical calibration point the origin for the tracker's location and orientation


	auto Location = TrackerLocation - PhysicalOrigin.GetTranslation();
	auto Orientation = TrackerRotation.Quaternion();
	//auto Orientation = PhysicalOrigin.InverseTransformRotation(TrackerRotation.Quaternion());
	//auto Orientation = TrackerRotation.Quaternion();
	// The location and orientation are now representative only of the movement and rotation that was done after the calibration.

    // To account for potential differences in device coordinate space based on the room it is used in,
	// a singleton has been introduced which stores a "Room Correction Transform", which is used to counteract these differences
	// Generally there shouldn't be any problems with this, unless the singleton for the project was just changed
	FTransform RoomTransform;
    auto VPSingleton = Cast<UVPSingleton>(GEngine->GameSingleton);

    if (VPSingleton)
    {
		// TODO: The room transform needs to be exposed via a UI element at a later date, rather than having it hard-coded here
		VPSingleton->RoomCorrection.SetRotation(FQuat::MakeFromEuler(FVector(0.0f, 0.0f, 0.0f)));
		RoomTransform = VPSingleton->RoomCorrection.Inverse();
    }

	Location = RoomTransform.TransformPosition(Location);
	Orientation = RoomTransform.TransformRotation(Orientation);

	// Simple fix to increase the effect tracker movement has on the tracker
	// Probably not realistic for virtual production, so default to 1.0f
	Location = Location * MovementMultiplier;

	// The location and orientation are now facing at the same direction as a physical camera would, and is moved according to the tracker's movement
	// They are still located at the world origin however.


	// All that is left is applying the virtual origin transform to move from the world origin to the desired location in the level
	Location = VirtualOrigin.TransformPosition(Location);
	Orientation = VirtualOrigin.TransformRotation(Orientation);

	// Set the location and rotation of the actor after all the transformations
	SetActorLocationAndRotation(Location, Orientation);

}

void AVPCamera::Calibrate()
{
	// Ensure that the known tracker location and orientation are correct
	UpdateTrackerState();

	// Record the current device transform of the tracker as it will be necessary for later use
	PhysicalOrigin = FTransform(TrackerRotation, TrackerLocation);

	// The rotation offset transform is used by the blueprint to change the relative rotation of the camera
	// Without this, the rotation of the tracker does not translate to the expected rotation of the virtual camera
	RotationOffsetTransform = PhysicalOrigin;

	auto VPSingleton = Cast<UVPSingleton>(GEngine->GameSingleton);

    if (VPSingleton)
    {
		RotationOffsetTransform *= VPSingleton->RoomCorrection.Inverse();        
    }

	// Make a Transform out of the tracker data

	// Fetch the transform of the calibration point for the tracker
	// Default to using the world origin if none is defined.
	if (IsValid(TrackerCalibrationPoint))
	{
		VirtualOrigin = TrackerCalibrationPoint->GetActorTransform();
	}
	else
	{
		if (GEngine)
			GEngine->AddOnScreenDebugMessage(-1, 10.0f, FColor::Red, TEXT("No valid calibration point was found, resorting to using the world origin"));
		VirtualOrigin = FTransform();
	}

	// Alert the blueprints that inherit from this that they can now take their post-calibration steps
	OnCalibration();

	// Set the actor's location and rotation to the location and rotation of the virtual origin fetched earlier.
	SetActorLocation(VirtualOrigin.GetLocation());
	SetActorRotation(VirtualOrigin.GetRotation());
}

bool AVPCamera::ShouldTickIfViewportsOnly() const
{
	// Control the ticking via a boolean exposed in the editor. If the boolean is false, fallback to the default way to evaluate.
	return bTrackInEditor || Super::ShouldTickIfViewportsOnly();
}

// Called every frame
void AVPCamera::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

	// Retrieve the current device transform of the tracker
	UpdateTrackerState();

	// Update the virtual transform for the actor based on the calibration point and the tracker's current transform
	UpdateVirtualTransform();
}

// Called to bind functionality to input
void AVPCamera::SetupPlayerInputComponent(UInputComponent* PlayerInputComponent)
{
	Super::SetupPlayerInputComponent(PlayerInputComponent);

}

