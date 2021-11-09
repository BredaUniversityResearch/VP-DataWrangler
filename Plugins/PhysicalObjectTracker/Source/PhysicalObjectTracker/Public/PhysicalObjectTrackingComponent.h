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

private:
	void DebugCheckIfTrackingTargetExists() const;

	UPROPERTY(EditAnywhere)
	int32 m_CurrentTargetDeviceId{-1};
	UPROPERTY(EditAnywhere)
		UPhysicalObjectTrackingReferencePoint* m_Reference{nullptr};
	UPROPERTY(EditAnywhere)
		FRotator m_RotationalOffset {};
};