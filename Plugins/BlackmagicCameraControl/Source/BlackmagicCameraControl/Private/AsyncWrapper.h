#pragma once
#include "BlackmagicCameraControl.h"
#include "WinRT.h"

template<typename TClassType, typename TResultType>
struct AsyncWrapper
{
	using TargetFunction = void(TClassType::*)(const TResultType&);
	TClassType* m_Target;
	TargetFunction m_FnTarget;

	AsyncWrapper(TClassType* Target, TargetFunction FnTarget);

	void operator() (winrt::Windows::Foundation::IAsyncOperation<TResultType> AsyncOpResult, winrt::Windows::Foundation::AsyncStatus status);
};

template <typename TClassType, typename TResultType>
AsyncWrapper<TClassType, TResultType>::AsyncWrapper(TClassType* Target, TargetFunction FnTarget)
	: m_Target(Target)
	, m_FnTarget(FnTarget)
{
}

template <typename TClassType, typename TResultType>
void AsyncWrapper<TClassType, TResultType>::operator()(winrt::Windows::Foundation::IAsyncOperation<TResultType> AsyncOpResult, winrt::Windows::Foundation::AsyncStatus status)
{
	if (status == winrt::Windows::Foundation::AsyncStatus::Completed)
	{
		(m_Target->*m_FnTarget)(AsyncOpResult.GetResults());
	}
	else
	{
		UE_LOG(LogBlackmagicCameraControl, Warning, TEXT("Async operation failed"));
	}
}

