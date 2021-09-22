// Fill out your copyright notice in the Description page of Project Settings.


#include "LightController.h"

#include "Kismet/GameplayStatics.h"

#include "CentralLightController.h"

#include "IPropertyChangeListener.h"

// Sets default values for this component's properties
ULightController::ULightController()
{
	// Set this component to be initialized when the game starts, and to be ticked every frame.  You can turn these features
	// off to improve performance if you don't need them.
	PrimaryComponentTick.bCanEverTick = true;

	// ...
}




// Called when the game starts
void ULightController::BeginPlay()
{
	Super::BeginPlay();


	// ...
	
}

void ULightController::OnComponentCreated()
{
	Super::OnComponentCreated();

	auto OwningActor = GetOwner();

	TArray<AActor*> CentralLightControllerArr;

	UGameplayStatics::GetAllActorsOfClass(GetWorld(), ACentralLightController::StaticClass(), CentralLightControllerArr);

    if (CentralLightControllerArr.Num() > 1)
    {
		GEngine->AddOnScreenDebugMessage(-1, 300.0f, FColor::Red, "The level has more than one central light controller, please remove one to avoid inconsistent behaviour");
    }

	ACentralLightController* Central = nullptr;

    if (CentralLightControllerArr.Num() == 0)
    {
		FActorSpawnParameters SpawnParam;
		SpawnParam.Owner = nullptr;
		Central = GetWorld()->SpawnActor<ACentralLightController>(ACentralLightController::StaticClass(), SpawnParam);
    }
	else
	{
		Central = Cast<ACentralLightController>(CentralLightControllerArr[0]);
	}

	Central->RegisterLightController(this);

}


// Called every frame
void ULightController::TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction)
{
	Super::TickComponent(DeltaTime, TickType, ThisTickFunction);

	// ...
}

