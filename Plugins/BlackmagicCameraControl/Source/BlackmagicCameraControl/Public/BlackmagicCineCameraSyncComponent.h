#pragma once
#include "BMCCLens.h"
#include "BMCCVideo.h"

#include "BlackmagicCineCameraSyncComponent.generated.h"

class UCineCameraComponent;
UCLASS(ClassGroup = (VirtualProduction), meta = (BlueprintSpawnableComponent))
class UBlackmagicCineCameraSyncComponent : public UActorComponent
{
	GENERATED_BODY()
public:
	explicit UBlackmagicCineCameraSyncComponent(const FObjectInitializer& ObjectInitializer);
	virtual void TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction) override;

	virtual void Activate(bool bReset) override;
	virtual void Deactivate() override;

private:
	UFUNCTION()
	void OnLensFocus(int32 SourceDevice, const FBMCCLens_Focus& Data);
	UFUNCTION()
	void OnVideoVideoMode(int32 SourceDevice, const FBMCCVideo_VideoMode& VideoMode);
	UFUNCTION()
	void OnRecordingFormat(int32 SourceDevice, const FBMCCVideo_RecordingFormat& RecordingFormat);

	UFUNCTION()
	void SubscribeToEvents();
private:
	bool IsSubscribed{ false };
	UCineCameraComponent* Target{ nullptr };
};