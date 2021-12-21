#include "BMCCDispatcher.h"

void UBMCCDispatcher::OnBatteryStatus(const FBMCCBattery_Info& BatteryInfo)
{
	BatteryStatusReceived.Broadcast(BatteryInfo);
}

void UBMCCDispatcher::OnMediaTransportMode(const FBMCCMedia_TransportMode& TransportMode)
{
	MediaTransportModeReceived.Broadcast(TransportMode);
}
