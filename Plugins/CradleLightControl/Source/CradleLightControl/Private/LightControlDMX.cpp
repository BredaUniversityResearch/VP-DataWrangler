
#include "LightControlDMX.h"
#include "IO/DMXOutputPort.h"

void SLightControlDMX::Construct(const FArguments& Args)
{
    CoreToolPtr = Args._CoreToolPtr;

    ChildSlot
        [
            SAssignNew(PortSelector, SDMXPortSelector)
            .Mode(EDMXPortSelectorMode::SelectFromAvailableInputsAndOutputs)
            .OnPortSelected_Lambda([this]()
            {
                    auto OutputPort = PortSelector->GetSelectedOutputPort();
                    if (OutputPort)
                    {
                        TMap<int32, uint8> Channels;
                        Channels.Emplace(1, 128);
                        Channels.Emplace(2, 128);
                        OutputPort->SendDMX(1, Channels);
                        GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Black, OutputPort->GetPortGuid().ToString());
                        //GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Black, "Yeet");
                    }
            })
        ];
}
