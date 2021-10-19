#pragma once

#include "Slate.h"
#include "Widgets/SDMXPortSelector.h"

class SLightControlDMX : public SCompoundWidget
{
public:

SLATE_BEGIN_ARGS(SLightControlDMX) {}

SLATE_ARGUMENT(class SLightControlTool*, CoreToolPtr)

SLATE_END_ARGS()

void Construct(const FArguments& Args);

private:

    SLightControlTool* CoreToolPtr;

    TSharedPtr<SDMXPortSelector> PortSelector;
};
