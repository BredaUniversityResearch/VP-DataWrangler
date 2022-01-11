#include "BlackmagicCineCameraSyncComponent.h"

#include "BlackmagicCameraControl.h"
#include "CineCameraComponent.h"

UBlackmagicCineCameraSyncComponent::UBlackmagicCineCameraSyncComponent(const FObjectInitializer& ObjectInitializer)
{
	PrimaryComponentTick.bCanEverTick = true;
	bTickInEditor = true;
	bAutoActivate = true;
}

void UBlackmagicCineCameraSyncComponent::TickComponent(float DeltaTime, ELevelTick TickType, FActorComponentTickFunction* ThisTickFunction)
{
	Super::TickComponent(DeltaTime, TickType, ThisTickFunction);
}

void UBlackmagicCineCameraSyncComponent::Activate(bool bReset)
{
	Super::Activate(bReset);
	UCineCameraComponent* component = GetOwner()->FindComponentByClass<UCineCameraComponent>();
	Target = component;
	if (component == nullptr)
	{
		UE_LOG(LogBlackmagicCameraControl, Error, TEXT("Could not find CineCameraComponent on attached actor"));
	}

	SubscribeToEvents();
}

void UBlackmagicCineCameraSyncComponent::Deactivate()
{
	Super::Deactivate();

	__debugbreak();
}

void UBlackmagicCineCameraSyncComponent::OnLensFocus(int32 SourceDevice, const FBMCCLens_Focus& Data)
{

}

void UBlackmagicCineCameraSyncComponent::OnVideoVideoMode(int32 SourceDevice, const FBMCCVideo_VideoMode& VideoMode)
{
	int a = 6;
}

void UBlackmagicCineCameraSyncComponent::OnRecordingFormat(int32 SourceDevice, const FBMCCVideo_RecordingFormat& RecordingFormat)
{
	FCameraFilmbackSettings& filmbackSettings = Target->Filmback;
	//filmbackSettings.SensorWidth = RecordingFormat.FrameWidthPixels;
	//filmbackSettings.SensorHeight = RecordingFormat.FrameHeightPixels;
	filmbackSettings.SensorAspectRatio = RecordingFormat.FrameWidthPixels / RecordingFormat.FrameHeightPixels;
}

void UBlackmagicCineCameraSyncComponent::SubscribeToEvents()
{
	FBMCCService& service = IModularFeatures::Get().GetModularFeature<FBMCCService>(FBMCCService::GetModularFeatureName());
	UBMCCDispatcher* dispatcher = service.GetDefaultDispatcher();
	dispatcher->LensFocusReceived.AddDynamic(this, &UBlackmagicCineCameraSyncComponent::OnLensFocus);
	dispatcher->VideoVideoMode.AddDynamic(this, &UBlackmagicCineCameraSyncComponent::OnVideoVideoMode);
	dispatcher->VideoRecordingFormat.AddDynamic(this, &UBlackmagicCineCameraSyncComponent::OnRecordingFormat);
}
