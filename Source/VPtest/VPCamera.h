// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Pawn.h"
#include "TrackerCalibrationPointCpp.h"
#include "GameFramework/SpringArmComponent.h"

#include "VPCamera.generated.h"

// The intended actor to represent a virtual camera connected to a Vive tracker
UCLASS(Blueprintable)
class VPTEST_API AVPCamera : public APawn
{
	GENERATED_BODY()

	

public:
	// Sets default values for this pawn's properties
	AVPCamera();

protected:
	// Called when the game starts or when spawned
	virtual void BeginPlay() override;

	UFUNCTION()
	virtual void OnConstruction(const FTransform& Transform) override;

	// Fetches the selected tracker's transform in physical space.
	// Results are stored in TrackerLocation and TrackerRotation.
	UFUNCTION(BlueprintCallable)
	void UpdateTrackerState();

	// Transform the actor to its virtual location and orientation relative to the virtual origin and the tracker's movement.
	UFUNCTION(BlueprintCallable)
	void UpdateVirtualTransform();

	// Calibrates the object and moves it to the virtual calibration point.
    // Resets the PhysicalOrigin to the current transform of the tracker. Sets the VirtualOrigin to the transform of the selected calibration point.
	UFUNCTION(BlueprintCallable, CallInEditor, Category = "Calibration")
	void Calibrate();

	// Blueprint implementable function to allow blueprints to react to the event of calibration. This is called BEFORE
	// the transform is set to the virtual origin.
	UFUNCTION(BlueprintImplementableEvent)
	void OnCalibration();

	// Needs to be overwritten to allow for the actor to tick in the editor as well
	UFUNCTION()
	bool ShouldTickIfViewportsOnly() const override;

	// Should a calibration be performed anytime the properties of the actor are changed in the editor
	// rather than only with the calibration button
	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Calibration")
	bool CalibrateOnPropertyChange = true;

	// This is necessary if the calibration point is a blueprint actor. Should be assigned via the defaults of the blueprint class.
	UPROPERTY(EditDefaultsOnly, BlueprintReadOnly, Category = "Calibration")
	TSubclassOf<ATrackerCalibrationPointCpp> CalibrationPointClass = ATrackerCalibrationPointCpp::StaticClass();
		
	UPROPERTY(BlueprintReadOnly, Category = "Calibration")
	FTransform RotationOffsetTransform;

	// Defines the actor whose transform is to be used as the origin transform for the camera.
	// Currently only TrackerCalibrationPointCpp and its children can be used in the scenario extra functionality is needed on that side.
	UPROPERTY(EditAnywhere, Category = "Calibration")
	ATrackerCalibrationPointCpp* TrackerCalibrationPoint;

	// Defines the scale for movements between the physical tracker and the virtual actor. Default to 1 for 1:1 scale.
	UPROPERTY(EditAnywhere, Category = "Calibration")
	float MovementMultiplier = 1.0f;

	// Base spring arm component used to smoothen out the micro movements that are registered by the tracker
    UPROPERTY(EditAnywhere);
	USpringArmComponent* SpringArmComponent;

	// The physical distance from the tracker to the tracked object.  
	UPROPERTY(EditAnywhere, Category = "Spring Arm")
	FVector OffsetFromObjectToTracker;

	// The physical location of the tracker.
	UPROPERTY(BlueprintReadOnly)	
	FVector TrackerLocation;

	// The physical rotation of the tracker.
	UPROPERTY(BlueprintReadOnly)
	FRotator TrackerRotation;

	// The physical location of the tracker at the time of calibration.
	UPROPERTY(BlueprintReadOnly)
	FTransform PhysicalOrigin;

	// The virtual location that is saved during the calibration and used as origin for any consequent movements.
	UPROPERTY(BlueprintReadOnly)
	FTransform VirtualOrigin;

	// Defines whether the tracker's movements will be reflected in the level in the editor. Defaults to true.
	UPROPERTY(EditAnywhere, Category = "Tracking")
	bool bTrackInEditor = true;


public:	
	// Called every frame
	virtual void Tick(float DeltaTime) override;

	// Called to bind functionality to input
	virtual void SetupPlayerInputComponent(class UInputComponent* PlayerInputComponent) override;

	

};
