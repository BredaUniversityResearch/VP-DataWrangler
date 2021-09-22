// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "Components/ActorComponent.h"
#include "LightController.generated.h"



// Component that is to be attached to light actors in order to centralize all light controls
UCLASS( ClassGroup=(Custom), meta=(BlueprintSpawnableComponent) )
class ULightController : public UActorComponent
{
	GENERATED_BODY()

public:	
	// Sets default values for this component's properties
	ULightController();

protected:
	// Called when the game starts
	virtual void BeginPlay() override;

	virtual void OnComponentCreated() override;
	
public:

	UPROPERTY(EditAnywhere, BlueprintReadWrite, Category = "Identification")
	FString ControllerID = "Unassigned";

	// Called every frame
	virtual void TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction) override;

		
};
