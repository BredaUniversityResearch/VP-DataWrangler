#pragma once


#include "PhysicalObjectTrackingComponent.generated.h"

class UPhysicalObjectTrackingReferencePoint;
UCLASS(ClassGroup = (VirtualProduction), meta = (BlueprintSpawnableComponent))
class PHYSICALOBJECTTRACKER_API UPhysicalObjectTrackingComponent: public USceneComponent
{
	GENERATED_BODY()
public:
	explicit UPhysicalObjectTrackingComponent(const FObjectInitializer& ObjectInitializer);

	virtual void BeginPlay() override;
	virtual void TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction) override;

	UFUNCTION(CallInEditor)
	void SelectTracker();

	UPROPERTY(EditAnywhere)
	int32 CurrentTargetDeviceId{-1};

private:
	void DebugCheckIfTrackingTargetExists() const;
	UPROPERTY(EditAnywhere)
	UPhysicalObjectTrackingReferencePoint* Reference{nullptr};

	
};