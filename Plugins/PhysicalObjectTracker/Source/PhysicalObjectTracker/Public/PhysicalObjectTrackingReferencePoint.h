#pragma once

#include "PhysicalObjectTrackingReferencePoint.generated.h"

UCLASS(BlueprintType)
class PHYSICALOBJECTTRACKER_API UPhysicalObjectTrackingReferencePoint: public UDataAsset
{
	GENERATED_BODY()
public:
	void SetNeutralTransform(const FQuat& NeutralRotation, const FVector& NeutralPosition);

	const FQuat& GetNeutralRotationInverse() const;
	const FVector& GetNeutralOffset() const;

private:
	UPROPERTY(VisibleAnywhere)
	FQuat m_NeutralRotationInverse;
	UPROPERTY(VisibleAnywhere)
	FVector m_NeutralOffset;
};

