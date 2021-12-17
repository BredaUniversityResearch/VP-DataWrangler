#pragma once

#include "BMCCBattery_Info.h"
#include "BMCCCommandMeta.h"

struct FBMCCAllPackets
{
	static constexpr FBMCCCommandMeta AllMeta[] = {
		FBMCCCommandMeta::Create<BMCCBattery_Info>()
	};

	static const FBMCCCommandMeta* FindMetaForIdentifier(const BMCCCommandIdentifier& Identifier)
	{
		for (const auto& it : AllMeta)
		{
			if (it.CommandIdentifier == Identifier)
			{
				return &it;
			}
		}
		return nullptr;
	}
};
