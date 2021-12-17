#pragma once
#include "BMCCCommandIdentifier.h"

class FBMCCCommandMeta
{
public:
	const int PayloadSize;
	const BMCCCommandIdentifier CommandIdentifier;

	template<typename TCommandType>
	static constexpr FBMCCCommandMeta Create()
	{
		return FBMCCCommandMeta(TCommandType::Identifier, sizeof(TCommandType));
	}

private:
	constexpr FBMCCCommandMeta(BMCCCommandIdentifier Identifier, int PayloadSize)
		: PayloadSize(PayloadSize)
		, CommandIdentifier(Identifier)
	{
	}
};
