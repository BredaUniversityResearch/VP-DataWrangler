#pragma once

#include "BMCCDataReceivedHandler.h"

class BLACKMAGICCAMERACONTROL_API FBlackmagicCameraControlService: public IModularFeature, public IBMCCDataReceivedHandler
{
	class Pimpl;
public:
	static FName GetModularFeatureName()
	{
		static const FName FeatureName = FName(TEXT("BlackmagicCameraControlService"));
		return FeatureName;
	}

	FBlackmagicCameraControlService();
	virtual ~FBlackmagicCameraControlService() override;

	virtual void OnDataReceived(BMCCDeviceHandle Source, const BMCCCommandHeader& Header,
		const TArrayView<uint8_t>& SerializedData) override;

private:
	TUniquePtr<Pimpl> m_Data;
};