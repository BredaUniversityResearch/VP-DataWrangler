#include "BMCCDispatcher.h"

void UBMCCDispatcher::OnBatteryStatus(BMCCDeviceHandle Source, const FBMCCBattery_Info& BatteryInfo)
{
	BatteryStatusReceived.Broadcast(BatteryInfo);
}

void UBMCCDispatcher::OnMediaTransportMode(BMCCDeviceHandle Source, const FBMCCMedia_TransportMode& TransportMode)
{
	MediaTransportModeReceived.Broadcast(TransportMode);
}
