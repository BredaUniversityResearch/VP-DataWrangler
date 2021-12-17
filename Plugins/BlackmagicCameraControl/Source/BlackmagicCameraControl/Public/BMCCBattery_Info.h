#pragma once
#include "BMCCCommandIdentifier.h"

struct BMCCBattery_Info
{
	static constexpr BMCCCommandIdentifier Identifier = BMCCCommandIdentifier(9, 0);

	int16 BatteryVoltage_mV;
	int16 BatteryPercentage;
	int16 Unknown;
};
static_assert(sizeof(BMCCBattery_Info) == 6, "BMCCBattery_Info is expected to contain 3 int16s");
