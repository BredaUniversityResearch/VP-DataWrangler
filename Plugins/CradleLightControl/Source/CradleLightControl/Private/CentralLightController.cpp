 // Fill out your copyright notice in the Description page of Project Settings.


#include "CentralLightController.h"

// Sets default values
ACentralLightController::ACentralLightController()
{
 	// Set this actor to call Tick() every frame.  You can turn this off to improve performance if you don't need it.
	PrimaryActorTick.bCanEverTick = true;

}

// Called when the game starts or when spawned
void ACentralLightController::BeginPlay()
{
	Super::BeginPlay();
	
}

// Called every frame
void ACentralLightController::Tick(float DeltaTime)
{
	Super::Tick(DeltaTime);

}

 void ACentralLightController::RegisterLightController(ULightController* Controller)
 {
     // Unregister the controller first to avoid duplication in the case of Id changes
     UnregisterLightController(Controller);
     LightControllers[Controller->ControllerID].Add(Controller);
 }

 void ACentralLightController::UnregisterLightController(ULightController* Controller)
 {
     auto IdPtr = ControllerIdList.Find(Controller);

     if (IdPtr)
     {
         auto Id = *IdPtr;
         ControllerIdList.Remove(Controller);
         LightControllers[Id].Remove(Controller);
         if (LightControllers[Id].Num() == 0)
         {
             LightControllers.Remove(Id);
         }
     }
 }

