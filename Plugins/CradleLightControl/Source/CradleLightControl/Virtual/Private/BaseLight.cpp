#include "BaseLight.h"

#include "ItemHandle.h"
#include "LightControlTool.h"
#include "ToolData.h"

#include "Engine/SkyLight.h"
#include "Engine/SpotLight.h"
#include "Engine/DirectionalLight.h"
#include "Engine/PointLight.h"

#include "ItemHandle.h"


uint8 UBaseLight::LoadFromJson(TSharedPtr<FJsonObject> JsonObject)
{

    auto State = JsonObject->GetBoolField("State");

    SetEnabled(State);

    SetLightIntensity(JsonObject->GetNumberField("Intensity"));
    SetHue(JsonObject->GetNumberField("Hue"));
    SetSaturation(JsonObject->GetNumberField("Saturation"));
    SetUseTemperature(JsonObject->GetBoolField("UseTemperature"));
    SetTemperature(JsonObject->GetNumberField("Temperature"));

    return UItemHandle::ELoadingResult::Success;
}

void UBaseLight::BeginTransaction()
{
    
}

void UBaseLight::PostTransacted(const FTransactionObjectEvent& TransactionEvent)
{
    if (TransactionEvent.GetEventType() == ETransactionObjectEventType::UndoRedo)
    {
        auto MasterLight = Handle->ToolData->GetMasterLight();
        if (MasterLight)
            Handle->ToolData->CoreToolPtr->GetLightPropertyEditor().Pin()->UpdateSaturationGradient(MasterLight->Item->Hue);
            
    }
}

FLinearColor UBaseLight::GetRGBColor() const
{
    uint8 Hue8 = StaticCast<uint8>(Hue / 360.0f * 255.0f);
    uint8 Saturation8 = StaticCast<uint8>(Saturation * 255.0f);
    return FLinearColor::MakeFromHSV8(Hue8, Saturation8, 255);
}


void UBaseLight::SetEnabled(bool bNewState)
{
    bIsEnabled = bNewState;
}

void UBaseLight::SetLightIntensity(float NormalizedValue)
{
    SetLightIntensity(NormalizedValue);
}

void UBaseLight::SetHue(float NewValue)
{
    Hue = NewValue;

}

void UBaseLight::SetSaturation(float NewValue)
{
    Saturation = NewValue;
}

void UBaseLight::SetUseTemperature(bool NewState)
{
    bUseTemperature = NewState;

}

void UBaseLight::SetTemperature(float NormalizedValue)
{
    Temperature = NormalizedValue;
}

void UBaseLight::SetCastShadows(bool bState)
{

}

void UBaseLight::AddHorizontal(float Degrees)
{
    Horizontal += Degrees;
}

void UBaseLight::AddVertical(float Degrees)
{
    Vertical += Degrees;
}

void UBaseLight::SetInnerConeAngle(float NewValue)
{
    InnerAngle = NewValue;
}

void UBaseLight::SetOuterConeAngle(float NewValue)
{
    OuterAngle = NewValue;
}

TSharedPtr<FJsonObject> UBaseLight::SaveAsJson() const
{
    TSharedPtr<FJsonObject> JsonItem = MakeShared<FJsonObject>();

    JsonItem->SetBoolField("State", bIsEnabled);
    JsonItem->SetNumberField("Intensity", Intensity);
    JsonItem->SetNumberField("Hue", Hue);
    JsonItem->SetNumberField("Saturation", Saturation);
    JsonItem->SetBoolField("UseTemperature", bUseTemperature);
    JsonItem->SetNumberField("Temperature", Temperature);
    

    TSharedPtr<FJsonValue> JsonValue = MakeShared<FJsonValueObject>(JsonItem);
    return JsonItem;
}

