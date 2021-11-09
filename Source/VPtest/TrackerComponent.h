// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "TrackerCalibrationPointCpp.h"

#include "SteamVRFunctionLibrary.h"

#include "TrackerComponent.generated.h"


UCLASS( ClassGroup=(Custom), meta=(BlueprintSpawnableComponent) )
class VPTEST_API UTrackerComponent : public USceneComponent
{
	GENERATED_BODY()

public:	
	// Sets default values for this component's properties
	UTrackerComponent();

protected:
	// Called when the game starts
	virtual void BeginPlay() override;

public:	
	// Called every frame
	virtual void TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction) override;

	void UpdateVirtualTransform();

	UFUNCTION(BlueprintCallable)
	FTransform GetCurrentTrackerTransform();

	UFUNCTION(CallInEditor, BlueprintCallable, Category = "Calibration")
	void Calibrate();

	bool MovementCheckDistance(float Threshold);

	bool MovementCheckDirection(int32 PointCount, float AcceptanceThreshold = 0.8f);

	UPROPERTY(EditAnywhere, Category = "Tracking")
	bool bTrack = true;

	UPROPERTY()
	TArray<FVector> LocationBuffer;

	// The 0-based Id of the tracker that this component will gets its device transform from.
	UPROPERTY(EditAnywhere, Category = "Tracking")
	int32 DeviceId = 0;

	// The track of device to track with this component. Generally Controller or Other, with other representing tracking pucks.
	UPROPERTY(EditAnywhere, Category = "Tracking")
	ESteamVRTrackedDeviceType DeviceType = ESteamVRTrackedDeviceType::Other;

	// Device space rotation of the tracker
	UPROPERTY(BlueprintReadOnly, Category = "Tracking")
	FRotator TrackerRotation;
	
	// Device space location of the tracker
	UPROPERTY(BlueprintReadOnly, Category = "Tracking")
	FVector TrackerLocation;

	UPROPERTY(EditAnywhere)
	float MovementMultiplier = 1.0f;

	UPROPERTY(EditAnywhere, BlueprintReadOnly, Category = "Calibration")
	ATrackerCalibrationPointCpp* TrackerCalibrationPoint;

	// The device space transform that is used as the origin point for the tracker. Determined by the location of the tracker during calibration.
	UPROPERTY(BlueprintReadOnly, Category = "Calibration")
	FTransform DeviceSpaceOrigin;

	// The world space transform that is used as the virtual origin point for the actor affected by the tracker. Determined by the virtual calibration point during calibration.
	UPROPERTY(BlueprintReadOnly, Category = "Calibration")
	FTransform WorldSpaceOrigin;
};
