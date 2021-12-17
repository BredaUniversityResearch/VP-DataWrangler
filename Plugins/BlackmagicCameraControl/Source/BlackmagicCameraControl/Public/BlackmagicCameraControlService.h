#pragma once

class BLACKMAGICCAMERACONTROL_API FBlackmagicCameraControlService: public IModularFeature
{
	class Pimpl;
public:
	static FName GetModularFeatureName()
	{
		static const FName FeatureName = FName(TEXT("BlackMagicCameraControlService"));
		return FeatureName;
	}

	FBlackmagicCameraControlService();
	~FBlackmagicCameraControlService(); 

private:
	TUniquePtr<Pimpl> m_Data;
};