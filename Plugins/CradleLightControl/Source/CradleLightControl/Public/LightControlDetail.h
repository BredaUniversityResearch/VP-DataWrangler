#pragma once

#include "IDetailCustomization.h"

class FLightControlDetailCustomization : public IDetailCustomization
{
public:
    virtual void CustomizeDetails(IDetailLayoutBuilder& DetailBuilder) override;


    static TSharedRef<IDetailCustomization> MakeInstance();
};
