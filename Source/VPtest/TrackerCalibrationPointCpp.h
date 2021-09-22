// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "TrackerCalibrationPointCpp.generated.h"

// Actor that acts as a calibration point for the VPCamera
// Exists as a C++ class in the event that something needs to be on its side in C++
UCLASS()
class VPTEST_API ATrackerCalibrationPointCpp : public AActor
{
	GENERATED_BODY()
	
public:	
	// Sets default values for this actor's properties
	ATrackerCalibrationPointCpp();

protected:
	// Called when the game starts or when spawned
	virtual void BeginPlay() override;

public:	
	// Called every frame
	virtual void Tick(float DeltaTime) override;

};
