#pragma once

class BLACKMAGICCAMERACONTROL_API FBlackMagicCameraControlService: public IModularFeature
{
	class Pimpl;
public:
	static FName GetModularFeatureName()
	{
		static const FName FeatureName = FName(TEXT("BlackMagicCameraControlService"));
		return FeatureName;
	}

	FBlackMagicCameraControlService();
	~FBlackMagicCameraControlService(); 

private:
	TUniquePtr<Pimpl> m_Data;
};