#include "BMCCCommandMeta.h"

#include "BMCCBattery_Info.h"
#include "BMCCLens.h"
#include "BMCCMedia_TransportMode.h"

#include "BlackmagicCameraControl.h"

namespace
{
	template<typename TCommandType, void(IBMCCCallbackHandler::* TDispatchFunction)(const TCommandType&)>
	void DispatchWrapperImpl(const TArray<IBMCCCallbackHandler*>& Listeners, const TArrayView<uint8>& SerializedDataView)
	{
		ensureMsgf(SerializedDataView.Num() == sizeof(TCommandType), TEXT("Could not deserialize message. Data size mismatch. Got %i expected %i"), SerializedDataView.Num(), sizeof(TCommandType));
		if (TDispatchFunction != nullptr)
		{
			const TCommandType* data = reinterpret_cast<const TCommandType*>(SerializedDataView.GetData());
			for (IBMCCCallbackHandler* handler : Listeners)
			{
				(handler->*TDispatchFunction)(*data);
			}
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

	Create<FBMCCBattery_Info, &IBMCCCallbackHandler::OnBatteryStatus>(),
	Create<FBMCCMedia_TransportMode, &IBMCCCallbackHandler::OnMediaTransportMode>()
};

template<typename TCommandType, void(IBMCCCallbackHandler::* TDispatchFunction)(const TCommandType&)>
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

void FBMCCCommandMeta::DeserializeAndDispatch(const TArray<IBMCCCallbackHandler*>& Array, const TArrayView<uint8>& ArrayView) const
{
	DispatchWrapper(Array, ArrayView);
}
