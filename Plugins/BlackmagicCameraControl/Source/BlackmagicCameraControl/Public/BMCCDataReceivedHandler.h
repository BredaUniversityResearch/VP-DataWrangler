#pragma once

#include "BMCCDeviceHandle.h"

struct BMCCCommandHeader;

class IBMCCDataReceivedHandler
{
public:
	virtual ~IBMCCDataReceivedHandler() = default;
	virtual void OnDataReceived(BMCCDeviceHandle Source, const BMCCCommandHeader& Header, const TArrayView<uint8_t>& SerializedData) = 0;
};
