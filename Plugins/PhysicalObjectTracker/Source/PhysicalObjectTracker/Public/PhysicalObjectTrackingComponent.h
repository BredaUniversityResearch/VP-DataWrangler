#pragma once


#include "PhysicalObjectTrackingComponent.generated.h"

class UPhysicalObjectTrackingReferencePoint;
UCLASS(ClassGroup = (VirtualProduction), meta = (BlueprintSpawnableComponent))
class PHYSICALOBJECTTRACKER_API UPhysicalObjectTrackingComponent: public UActorComponent
{
	GENERATED_BODY()
public:
	explicit UPhysicalObjectTrackingComponent(const FObjectInitializer& ObjectInitializer);
	virtual void OnRegister() override;
	virtual void BeginPlay() override;
	virtual void TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction) override;

	virtual void PostEditChangeProperty(FPropertyChangedEvent& PropertyChangedEvent) override;
	

	UFUNCTION(CallInEditor, Category = "PhysicalObjectTrackingComponent")
	void SelectTracker();

	UFUNCTION(CallInEditor, Category = "PhysicalObjectTrackingComponent")
	void RefreshDeviceId();

	UPROPERTY(Transient, VisibleAnywhere)
	int32 CurrentTargetDeviceId{-1};

	UPROPERTY(EditAnywhere, meta=(DeviceSerialId))
	FString SerialId;

private:
	void DebugCheckIfTrackingTargetExists() const;
	UPROPERTY(EditAnywhere)
	UPhysicalObjectTrackingReferencePoint* TrackingSpaceReference{nullptr};
	UPROPERTY(EditInstanceOnly)
	AActor* WorldReferencePoint{nullptr};

	UPROPERTY(Transient)
	float DeltaTimeAccumulator;

	UPROPERTY()
	float TimeoutLimit {1.0f};
};