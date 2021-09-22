// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "SteamVRFunctionLibrary.h"


#include "TrackerBase.generated.h"

// This was a prototype for a different type of calibration
// The goal was to selectively enable and disable the tracking,
// So in theory, the fastest and easiest way to calibrate it would be to place the physical camera in its calibration point,
// while disabling the tracking on this actor and transforming it to its virtual origin. Then, upon continuing tracking, it would
// collect new device and virtual origin points and use them, thus avoiding random teleportations.
//
// I reached the conclusion that this is more work in the long run than the calibration point system used by the VPCamera class
// I intend to reuse the device selection setup from this class when fixing it for VPCamera. Could also reuse the whole enable-disable
// thing for the tracking, as it has no real point of conflict with the other method, and having both can't really hurt



USTRUCT(BlueprintType)
struct FTrackingInfo
{
	GENERATED_BODY()

	// The type of the device that is to be used for the position and orientation of this tracker.
	// Vive Trackers are generally considered as "Other"
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Tracking")
		ESteamVRTrackedDeviceType DeviceType = ESteamVRTrackedDeviceType::Controller;

	// The zero-based index of the device to track within its type.
	// The index must be in the range from 0 to (Number of Devices - 1)
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Tracking", meta = (ClampMin = 0));
	int DeviceIndex = 0;

	// Should the actor move together with the tracked device?
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Tracking");
	bool Track = true;

	// Should the device be tracked in the viewport as well, or only during playtime
	UPROPERTY(EditAnywhere, Category = "Tracking")
		bool TrackInViewport = true;

	UPROPERTY()
		bool Calibrated = false;
};

UCLASS()
class VPTEST_API ATrackerBase : public AActor
{
	GENERATED_BODY()
	
public:	
	// Sets default values for this actor's properties
	ATrackerBase();

protected:
	// Called when the game starts or when spawned
	virtual void BeginPlay() override;

	// Construction script equivalent
	void OnConstruction(const FTransform& Transform) override;

	// Moves the actor in relationship to the tracker's movement
	UFUNCTION(BlueprintCallable)
	void UpdateVirtualPosition();

	// Sets the device space origin for the tracker and its world space counterpart
	UFUNCTION(BlueprintCallable)
	void Calibrate();

	// Updates the device space transform taking into account the device space origin set during calibration
	UFUNCTION(BlueprintCallable)
	void UpdateTrackerTransform();

	// Returns the unmodified device space transform of the tracker
	UFUNCTION(BlueprintCallable)
	FTransform GetTrackerTransformRaw();

public:

	UPROPERTY(EditAnywhere, Category = "Tracking")
	FTrackingInfo TrackingInfo;

	// Used to trigger tracking-related updates only when the info is changed
	UPROPERTY()
	FTrackingInfo OldTrackingInfo;

	UPROPERTY(BlueprintReadOnly)
		FTransform DeviceSpaceOrigin;

	UPROPERTY(BlueprintReadOnly)
		FTransform WorldSpaceOrigin;

	UPROPERTY(BlueprintReadOnly);
	    FTransform DeviceSpaceTransform;

	// Called every frame
	virtual void Tick(float DeltaTime) override;

	// Override to allow for the actor to tick without the rest of the leveling ticking
	UFUNCTION()
	bool ShouldTickIfViewportsOnly() const override;

};

bool operator!=(FTrackingInfo Left, FTrackingInfo Right);
