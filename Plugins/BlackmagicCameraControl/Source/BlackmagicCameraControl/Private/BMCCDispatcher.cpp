#include "BMCCDispatcher.h"

void UBMCCDispatcher::OnBatteryStatus(const FBMCCBattery_Info& a_BatteryInfo)
{
	BatteryStatusReceived.Broadcast(a_BatteryInfo);
}
