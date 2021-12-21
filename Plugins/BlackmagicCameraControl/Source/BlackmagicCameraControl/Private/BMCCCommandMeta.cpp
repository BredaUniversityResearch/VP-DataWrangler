#include "BMCCCommandMeta.h"

#include "BMCCBattery_Info.h"

namespace
{
	template<typename TCommandType, void(IBMCCCallbackHandler::* TDispatchFunction)(const TCommandType&)>
	void DispatchWrapperImpl(const TArray<IBMCCCallbackHandler*>& Listeners, const TArrayView<uint8>& SerializedDataView)
	{
		ensureMsgf(SerializedDataView.Num() == sizeof(TCommandType), TEXT("Could not deserialize message. Data size mismatch. Got %i expected %i"), SerializedDataView.Num(), sizeof(TCommandType));
		const TCommandType* data = reinterpret_cast<const TCommandType*>(SerializedDataView.GetData());
		for (IBMCCCallbackHandler* handler : Listeners)
		{
			(handler->*TDispatchFunction)(*data);
		}
	}
};

const FBMCCCommandMeta FBMCCCommandMeta::m_AllMeta[] = {
	Create<FBMCCBattery_Info, &IBMCCCallbackHandler::OnBatteryStatus>()
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
