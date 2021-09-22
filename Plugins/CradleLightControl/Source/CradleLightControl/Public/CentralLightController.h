// Fill out your copyright notice in the Description page of Project Settings.

#pragma once

#include "CoreMinimal.h"
#include "GameFramework/Actor.h"
#include "LightController.h"

#include "CentralLightController.generated.h"

UCLASS()
class ACentralLightController : public AActor
{
	GENERATED_BODY()
	
public:

	DECLARE_DELEGATE(FLightControlDetailsDelegate);

	// Sets default values for this actor's properties
	ACentralLightController();

protected:
	// Called when the game starts or when spawned
	virtual void BeginPlay() override;

	TMap<FString, TSet<ULightController*>> LightControllers;
	TMap<ULightController*, FString> ControllerIdList;


public:	
	// Called every frame
	virtual void Tick(float DeltaTime) override;

	UFUNCTION(BlueprintCallable)
	void RegisterLightController(ULightController* Controller);

	void UnregisterLightController(ULightController* Controller);
};
