#include "BMCCCommandMeta.h"

#include "BMCCBattery_Info.h"
#include "BMCCLens.h"
#include "BMCCMedia_TransportMode.h"

#include "BlackmagicCameraControl.h"
#include "BMCCVideo.h"

namespace
{
	template<typename TCommandType, void(IBMCCCallbackHandler::* TDispatchFunction)(BMCCDeviceHandle, const TCommandType&)>
	void DispatchWrapperImpl(IBMCCCallbackHandler* DispatchTarget, BMCCDeviceHandle Source, const TArrayView<uint8>& SerializedDataView)
	{
		ensureMsgf(SerializedDataView.Num() == sizeof(TCommandType), TEXT("Could not deserialize message. Data size mismatch. Got %i expected %llu"), SerializedDataView.Num(), sizeof(TCommandType));
		if (TDispatchFunction != nullptr)
		{
			const TCommandType* data = reinterpret_cast<const TCommandType*>(SerializedDataView.GetData());
			(DispatchTarget->*TDispatchFunction)(Source, *data);
		}
		else 
		{
			UE_LOG(LogBlackmagicCameraControl, Error, TEXT("Deserialize and Dispatch got message that has nullptr dispatch. This should not happen I think"));
		}
	}
};

const FBMCCCommandMeta FBMCCCommandMeta::m_AllMeta[] = {
	Create<FBMCCLens_Focus, &IBMCCCallbackHandler::OnLensFocus>(),
	Create<FBMCCLens_TriggerAutoFocus, nullptr>(),
	Create<FBMCCLens_ApertureFStop, &IBMCCCallbackHandler::OnLensApertureFStop>(),
	Create<FBMCCLens_ApertureNormalized, &IBMCCCallbackHandler::OnLensApertureNormalized>(),
	Create<FBMCCLens_ApertureOrdinal, &IBMCCCallbackHandler::OnLensApertureOrdinal>(),
	Create<FBMCCLens_TriggerInstantAutoAperture, nullptr>(),
	Create<FBMCCLens_OpticalImageStabilization, &IBMCCCallbackHandler::OnLensOpticalImageStabilization>(),
	Create<FBMCCLens_SetAbsoluteZoomMm, &IBMCCCallbackHandler::OnLensAbsoluteZoomMm>(),
	Create<FBMCCLens_SetAbsoluteZoomNormalized, &IBMCCCallbackHandler::OnLensAbsoluteZoomNormalized>(),
	Create<FBMCCLens_SetContinuousZoom, &IBMCCCallbackHandler::OnLensContinuousZoom>(),

	Create<FBMCCVideo_VideoMode, &IBMCCCallbackHandler::OnVideoVideoMode>(),
	Create<FBMCCVideo_Gain, &IBMCCCallbackHandler::OnVideoGain>(),
	Create<FBMCCVideo_RecordingFormat, &IBMCCCallbackHandler::OnVideoRecordingFormat>(),

	Create<FBMCCBattery_Info, &IBMCCCallbackHandler::OnBatteryStatus>(),
	Create<FBMCCMedia_TransportMode, &IBMCCCallbackHandler::OnMediaTransportMode>()
};

template<typename TCommandType, void(IBMCCCallbackHandler::* TDispatchFunction)(BMCCDeviceHandle, const TCommandType&)>
FBMCCCommandMeta FBMCCCommandMeta::Create()
{
	return FBMCCCommandMeta(TCommandType::Identifier, sizeof(TCommandType), &DispatchWrapperImpl<TCommandType, TDispatchFunction>);
}

const FBMCCCommandMeta* FBMCCCommandMeta::FindMetaForIdentifier(const FBMCCCommandIdentifier& Identifier)
{
	for (const auto& it : m_AllMeta)
	{
		if (it.CommandIdentifier == Identifier)
		{
			return &it;
		}
	}
	return nullptr;
}

void FBMCCCommandMeta::DeserializeAndDispatch(IBMCCCallbackHandler* DispatchTarget, BMCCDeviceHandle Source, const TArrayView<uint8>& ArrayView) const
{
	DispatchWrapper(DispatchTarget, Source, ArrayView);
}
