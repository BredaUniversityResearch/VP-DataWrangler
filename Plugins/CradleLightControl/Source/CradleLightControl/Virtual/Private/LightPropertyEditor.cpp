#include "LightPropertyEditor.h"
#include "LightControlTool.h"

#include "ToolData.h"
#include "ItemHandle.h"
#include "BaseLight.h"

TArray<FColor> SLightPropertyEditor::LinearGradient(TArray<FColor> ControlPoints, FVector2D Size,
    EOrientation Orientation)
{
    TArray<FColor> GradientPixels;
    if (ControlPoints.Num())
    {
        if (ControlPoints.Num() == 1)
        {
            for (size_t x = 0; x < Size.X; x++)
            {
                for (size_t y = 0; y < Size.Y; y++)
                {
                    GradientPixels.Add(ControlPoints[0]);
                }
            }
        }
        else
        {
            auto NumSteps = ControlPoints.Num() - 1;
            int TotalStepSize;
            int RepeatCount;
            if (Orientation == Orient_Vertical)
            {
                TotalStepSize = Size.Y;
                RepeatCount = Size.X;
            }
            else
            {
                TotalStepSize = Size.X;
                RepeatCount = Size.Y;
            }
            auto StepSize = TotalStepSize / NumSteps;

            for (auto Rep = 0; Rep < RepeatCount; Rep++)
            {
                for (auto Pixel = 0; Pixel < TotalStepSize; Pixel++)
                {
                    auto Progress = StaticCast<float>(Pixel) / StaticCast<float>(StepSize);
                    auto BeforeId = StaticCast<int>(FMath::Floor(Progress)); // Avoid setting Bef
                    auto Alpha = FMath::Frac(Progress);
                    FLinearColor Before, After;
                    Before = ControlPoints[BeforeId];
                    After = ControlPoints[FMath::Min(ControlPoints.Num() - 1, BeforeId + 1)];

                    auto lc = ((1.0f - Alpha) * Before + Alpha * After);
                    FColor c(lc.ToFColor(false));
                    GradientPixels.Add(c);

                }
            }
        }
    }

    return GradientPixels;
}

UTexture2D* SLightPropertyEditor::MakeGradientTexture(int X, int Y)
{
    auto Tex = UTexture2D::CreateTransient(X, Y);
    Tex->CompressionSettings = TextureCompressionSettings::TC_VectorDisplacementmap;
    Tex->SRGB = 0;
    Tex->AddToRoot();
    Tex->UpdateResource();
    return Tex;
}

void SLightPropertyEditor::Construct(const FArguments& Args)
{
    _ASSERT(Args._ToolData);
    ToolData = Args._ToolData;

    GenerateTextures();


    SVerticalBox::FSlot* IntensityNameSlot, *IntensityValueSlot, *IntensityPercentageSlot;
    SVerticalBox::FSlot* HueNameSlot, *HueValueSlot, *HuePercentageSlot;
    SVerticalBox::FSlot* SaturationNameSlot, *SaturationValueSlot, *SaturationPercentageSlot;
    SVerticalBox::FSlot* TemperatureNameSlot, * TemperatureCheckboxSlot, *TemperatureValueSlot, *TemperaturePercentageSlot;


    ChildSlot
    [
        SNew(SHorizontalBox)
        +SHorizontalBox::Slot() // Intensity slider
        .VAlign(VAlign_Fill)
        [
            SNew(SBorder)
            .VAlign(VAlign_Fill)
            .HAlign(HAlign_Fill)
            .ColorAndOpacity(FLinearColor::Gray)
            [
                SNew(SVerticalBox)
                +SVerticalBox::Slot()
                .Expose(IntensityNameSlot)
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("Intensity"))
                ]
                +SVerticalBox::Slot()
                .Expose(IntensityValueSlot)
                [
                    SNew(STextBlock)
                    .Text(this, &SLightPropertyEditor::GetIntensityValueText)
                ]
                +SVerticalBox::Slot()
                [
                    SNew(SHorizontalBox)
                    + SHorizontalBox::Slot()
                    .Padding(3.0f, 0.0f)
                    .HAlign(HAlign_Right)
                    [
                        SNew(SBorder)
                        [
                            SNew(SImage)
                            .Image(IntensityGradientBrush.Get())
                        ]
                    ]
                    +SHorizontalBox::Slot()
                    .Padding(3.0f, 0.0f)
                    .HAlign(HAlign_Left)
                    [
                        SNew(SSlider)
                        .Orientation(EOrientation::Orient_Vertical)
                        .Value(this, &SLightPropertyEditor::GetIntensityValue)
                        .OnValueChanged(this, &SLightPropertyEditor::OnIntensityValueChanged)
                        .OnMouseCaptureBegin(this, &SLightPropertyEditor::IntensityTransactionBegin)
                        .OnMouseCaptureEnd(this, &SLightPropertyEditor::EndTransaction)
                    ]
                ]
                +SVerticalBox::Slot()
                .Expose(IntensityPercentageSlot)
                [
                    SNew(STextBlock)
                    .Justification(ETextJustify::Center)
                    .Text(this, &SLightPropertyEditor::GetIntensityPercentage)
                ]
            ]
        ]
        + SHorizontalBox::Slot()
            .VAlign(VAlign_Fill)
        [
            SNew(SBorder)
            .VAlign(VAlign_Fill)
            .HAlign(HAlign_Fill)
            .ColorAndOpacity(FLinearColor::Gray)
            [
                SNew(SVerticalBox)
                +SVerticalBox::Slot()
                .Expose(HueNameSlot)
                [
                    SNew(STextBlock)
                    .Justification(ETextJustify::Center)
                    .Text(FText::FromString("Hue"))
                ]
                +SVerticalBox::Slot()
                .Expose(HueValueSlot)
                [
                    SNew(STextBlock)
                    .Justification(ETextJustify::Center)
                    .Text(this, &SLightPropertyEditor::GetHueValueText)
                ]
                +SVerticalBox::Slot()
                [
                    SNew(SHorizontalBox)
                    + SHorizontalBox::Slot()
                    .HAlign(HAlign_Right)
                    [
                        SNew(SImage)
                        .Image(HSVGradientBrush.Get())
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Left)
                    [
                        SNew(SSlider)
                        .Orientation(EOrientation::Orient_Vertical)
                        .Value(this, &SLightPropertyEditor::GetHueValue)
                        .OnValueChanged_Raw(this, &SLightPropertyEditor::OnHueValueChanged)
                        .OnMouseCaptureBegin(this, &SLightPropertyEditor::HueTransactionBegin)
                        .OnMouseCaptureEnd(this, &SLightPropertyEditor::EndTransaction)
                    ]
                ]
                +SVerticalBox::Slot()
                .Expose(HuePercentageSlot)
                [
                    SNew(STextBlock)
                    .Justification(ETextJustify::Center)
                    .Text(this, &SLightPropertyEditor::GetHuePercentage)
                ]
            ]
        ]
        + SHorizontalBox::Slot()
            .VAlign(VAlign_Fill)
        [
            SNew(SBorder)
            .VAlign(VAlign_Fill)
            .HAlign(HAlign_Fill)
            .ColorAndOpacity(FLinearColor::Gray)
            [
                SNew(SVerticalBox)
                +SVerticalBox::Slot()
                .Expose(SaturationNameSlot)
                [
                    SNew(STextBlock)
                    .Justification(ETextJustify::Center)
            .Text(FText::FromString("Saturation"))
                ]
                +SVerticalBox::Slot()
                .Expose(SaturationValueSlot)
                [
                    SNew(STextBlock)
                    .Justification(ETextJustify::Center)
                    .Text(this, &SLightPropertyEditor::GetSaturationValueText)
                ]
                +SVerticalBox::Slot()
                [
                    SNew(SHorizontalBox)
                    + SHorizontalBox::Slot()
                    .HAlign(HAlign_Right)
                    [
                        SNew(SImage)
                        .Image_Raw(this, &SLightPropertyEditor::GetSaturationGradientBrush)
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Left)
                    [
                        SNew(SSlider)
                        .Orientation(EOrientation::Orient_Vertical)
                        .Value_Raw(this, &SLightPropertyEditor::GetSaturationValue)
                        .OnValueChanged_Raw(this, &SLightPropertyEditor::OnSaturationValueChanged)
                        .OnMouseCaptureBegin(this, &SLightPropertyEditor::SaturationTransactionBegin)
                        .OnMouseCaptureEnd(this, &SLightPropertyEditor::EndTransaction)
                    ]
                ]
                +SVerticalBox::Slot()
                .Expose(SaturationPercentageSlot)
                [
                    SNew(STextBlock)
                    .Justification(ETextJustify::Center)
                    .Text(this, &SLightPropertyEditor::GetSaturationValueText) // The content is the same as the value text
                ]
            ]
        ]
        + SHorizontalBox::Slot()
            .VAlign(VAlign_Fill)
        [
            SNew(SBorder)
            .VAlign(VAlign_Fill)
            .HAlign(HAlign_Fill)
            .ColorAndOpacity(FLinearColor::Gray)
            [
                SNew(SVerticalBox)
                +SVerticalBox::Slot()
                .Expose(TemperatureNameSlot)
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("Temperature"))
                    .Justification(ETextJustify::Center)
                ]
                +SVerticalBox::Slot()
                .HAlign(HAlign_Center)
                .Expose(TemperatureCheckboxSlot)
                [
                    SNew(SCheckBox)
                    .ToolTipText(FText::FromString("Should the temperature be taken into account?"))
                    .OnCheckStateChanged(this, &SLightPropertyEditor::OnTemperatureCheckboxChecked)
                    .IsChecked(this, &SLightPropertyEditor::GetTemperatureCheckboxChecked)
                    .IsEnabled(this, &SLightPropertyEditor::TemperatureEnabled)
                ]
                +SVerticalBox::Slot()
                .Expose(TemperatureValueSlot)
                [
                    SNew(STextBlock)
                    .Text(this, &SLightPropertyEditor::GetTemperatureValueText)
                    .Justification(ETextJustify::Center)
                ]
                +SVerticalBox::Slot()
                [
                    SNew(SHorizontalBox)
                    + SHorizontalBox::Slot()
                    .HAlign(HAlign_Right)
                    [
                        SNew(SImage)
                        .Image(TemperatureGradientBrush.Get())
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Left)
                    [
                        SNew(SSlider)
                        .Orientation(EOrientation::Orient_Vertical)
                        .OnValueChanged(this, &SLightPropertyEditor::OnTemperatureValueChanged)
                        .Value(this, &SLightPropertyEditor::GetTemperatureValue)
                        .IsEnabled(this, &SLightPropertyEditor::TemperatureEnabled)
                        .OnMouseCaptureBegin(this, &SLightPropertyEditor::TemperatureTransactionBegin)
                        .OnMouseCaptureEnd(this, &SLightPropertyEditor::EndTransaction)
                    ]
                ]
                +SVerticalBox::Slot()
                .Expose(TemperaturePercentageSlot)
                [
                    SNew(STextBlock)
                    .Justification(ETextJustify::Center)
                    .Text(this, &SLightPropertyEditor::GetTemperaturePercentage)
                ]
            ]
        ]
    ];

    IntensityNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    IntensityValueSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    IntensityPercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    HueNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    HueValueSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    HuePercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    SaturationNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    SaturationValueSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    SaturationPercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    TemperatureNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    TemperatureCheckboxSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    TemperatureValueSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    TemperaturePercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

}

void SLightPropertyEditor::PreDestroy()
{
    IntensityGradientTexture->ConditionalBeginDestroy();
    IntensityGradientTexture->RemoveFromRoot();
    HSVGradientTexture->ConditionalBeginDestroy();
    HSVGradientTexture->RemoveFromRoot();
    DefaultSaturationGradientTexture->ConditionalBeginDestroy();
    DefaultSaturationGradientTexture->RemoveFromRoot();
    SaturationGradientTexture->ConditionalBeginDestroy();
    SaturationGradientTexture->RemoveFromRoot();
    TemperatureGradientTexture->ConditionalBeginDestroy();
    TemperatureGradientTexture->RemoveFromRoot();
}

void SLightPropertyEditor::GenerateTextures()
{
    IntensityGradientTexture = MakeGradientTexture();
    HSVGradientTexture = MakeGradientTexture();
    DefaultSaturationGradientTexture = MakeGradientTexture();
    SaturationGradientTexture = MakeGradientTexture();
    TemperatureGradientTexture = MakeGradientTexture();

    auto TextureGenLambda = [this](FRHICommandListImmediate& RHICmdList)
    {
        auto UpdateRegion = FUpdateTextureRegion2D(0, 0, 0, 0, 1, 256);


        TArray<FColor> IntensityGradientPixels = LinearGradient(TArray<FColor>{
            FColor::Black,
                FColor::White
        });

        TArray<FColor> HSVGradientPixels = LinearGradient(TArray<FColor>{
            FColor::Red,
                FColor::Magenta,
                FColor::Blue,
                FColor::Cyan,
                FColor::Green,
                FColor::Yellow,
                FColor::Red
        });

        TArray<FColor> SaturationGradientPixels = LinearGradient(TArray<FColor>{
            FColor::Red,
                FColor::White
        });

        TArray<FColor> TemperatureGradientPixels = LinearGradient(TArray<FColor>{
            FColor::Cyan,
                FColor::White,
                FColor::Yellow,
                FColor::Red
        });




        RHIUpdateTexture2D(IntensityGradientTexture->GetResource()->GetTexture2DRHI(), 0, UpdateRegion,
            sizeof(FColor), reinterpret_cast<uint8_t*>(IntensityGradientPixels.GetData()));


        RHIUpdateTexture2D(HSVGradientTexture->GetResource()->GetTexture2DRHI(), 0, UpdateRegion,
            sizeof(FColor), reinterpret_cast<uint8_t*>(HSVGradientPixels.GetData()));
        RHIUpdateTexture2D(DefaultSaturationGradientTexture->GetResource()->GetTexture2DRHI(), 0, UpdateRegion,
            sizeof(FColor), reinterpret_cast<uint8_t*>(SaturationGradientPixels.GetData()));
        RHIUpdateTexture2D(SaturationGradientTexture->GetResource()->GetTexture2DRHI(), 0, UpdateRegion, // Initialize the non-default saturation texture the same as the default one
            sizeof(FColor), reinterpret_cast<uint8_t*>(SaturationGradientPixels.GetData()));
        RHIUpdateTexture2D(TemperatureGradientTexture->GetResource()->GetTexture2DRHI(), 0, UpdateRegion,
            sizeof(FColor), reinterpret_cast<uint8_t*>(TemperatureGradientPixels.GetData()));

    };
    //EnqueueUniqueRenderCommand(l);
    ENQUEUE_RENDER_COMMAND(UpdateTextureDataCommand)(TextureGenLambda);
    FlushRenderingCommands();

    const FVector2D GradientSize(20.0f, 256.0f);

    IntensityGradientBrush = MakeShared<FSlateImageBrush>(IntensityGradientTexture, GradientSize);
    HSVGradientBrush = MakeShared<FSlateImageBrush>(HSVGradientTexture, GradientSize);
    DefaultSaturationGradientBrush = MakeShared<FSlateImageBrush>(DefaultSaturationGradientTexture, GradientSize);
    SaturationGradientBrush = MakeShared<FSlateImageBrush>(SaturationGradientTexture, GradientSize);
    TemperatureGradientBrush = MakeShared<FSlateImageBrush>(TemperatureGradientTexture, GradientSize);
}

void SLightPropertyEditor::UpdateSaturationGradient(float NewHue)
{
    ENQUEUE_RENDER_COMMAND(UpdateTextureDataCommand)([this, NewHue](FRHICommandListImmediate& RHICmdList)
        {
            auto UpdateRegion = FUpdateTextureRegion2D(0, 0, 0, 0, 1, 256);

            auto C = FLinearColor::MakeFromHSV8(StaticCast<uint8>(NewHue / 360.0f * 255.0f), 255, 255).ToFColor(false);
            auto NewGradient = LinearGradient(TArray<FColor>{
                C,
                    FColor::White
            });

            RHIUpdateTexture2D(SaturationGradientTexture->GetResource()->GetTexture2DRHI(), 0, UpdateRegion, sizeof(FColor), reinterpret_cast<uint8_t*>(NewGradient.GetData()));
        });
}

const FSlateBrush* SLightPropertyEditor::GetSaturationGradientBrush() const
{
    if (ToolData->IsAMasterLightSelected())
    {
        return SaturationGradientBrush.Get();
    }
    return DefaultSaturationGradientBrush.Get();
}


void SLightPropertyEditor::EndTransaction()
{
    GEditor->EndTransaction();
}

void SLightPropertyEditor::OnIntensityValueChanged(float Value)
{
    for (auto SelectedItem : ToolData->LightsUnderSelection)
    {
        SelectedItem->Item->BeginTransaction();
        SelectedItem->Item->SetLightIntensity(Value);
    }
}

void SLightPropertyEditor::IntensityTransactionBegin()
{
    auto MasterLight = ToolData->GetMasterLight();
    if (MasterLight)
        GEditor->BeginTransaction(FText::FromString(MasterLight->Name + " Intensity"));        
}


FText SLightPropertyEditor::GetIntensityValueText() const
{
    FString Res = "0";
    if (ToolData->IsAMasterLightSelected())
    {
        auto Light = ToolData->SelectionMasterLight;
        if (Light->Type == ETreeItemType::PointLight ||
            Light->Type == ETreeItemType::SpotLight)
        {
            Res = FString::FormatAsNumber(Light->Item->Intensity) + " Lumen";
        }
        else
            Res = "Currently not supported";
    }
    return FText::FromString(Res);
}

float SLightPropertyEditor::GetIntensityValue() const
{
    if (ToolData->IsAMasterLightSelected())
    {
        return ToolData->SelectionMasterLight->Item->GetIntensityNormalized();
    }
    return 0;
}

FText SLightPropertyEditor::GetIntensityPercentage() const
{
    FString Res = "0%";
    if (ToolData->IsAMasterLightSelected())
    {
        Res = FString::FormatAsNumber(ToolData->SelectionMasterLight->Item->GetIntensityNormalized() * 100.0f) + "%";
    }
    return FText::FromString(Res);
}

void SLightPropertyEditor::OnHueValueChanged(float Value)
{
    for (auto SelectedItem : ToolData->GetSelectedLights())
    {
        SelectedItem->BeginTransaction();
        SelectedItem->Item->SetHue(Value * 360.0f);
        UpdateSaturationGradient(Value * 360.0F);
    }
}

void SLightPropertyEditor::HueTransactionBegin()
{
    auto MasterLight = ToolData->GetMasterLight();
    if (MasterLight)
        GEditor->BeginTransaction(FText::FromString(MasterLight->Name + " Hue"));
}

FText SLightPropertyEditor::GetHueValueText() const
{
    FString Res = "0";
    if (ToolData->IsAMasterLightSelected())
    {
        Res = FString::FormatAsNumber(ToolData->SelectionMasterLight->Item->Hue);
    }
    return FText::FromString(Res);
}

float SLightPropertyEditor::GetHueValue() const
{
    if (ToolData->IsAMasterLightSelected())
    {
        return ToolData->SelectionMasterLight->Item->Hue / 360.0f;
    }
    return 0;
}

FText SLightPropertyEditor::GetHuePercentage() const
{
    FString Res = "0%";
    if (ToolData->IsAMasterLightSelected())
    {
        Res = FString::FormatAsNumber(ToolData->SelectionMasterLight->Item->Hue / 3.6f) + "%";
    }
    return FText::FromString(Res);
}

void SLightPropertyEditor::OnSaturationValueChanged(float Value)
{
    for (auto SelectedItem : ToolData->LightsUnderSelection)
    {
        SelectedItem->BeginTransaction();
        SelectedItem->Item->SetSaturation(Value);
        //SelectedItem->UpdateLightColor();
    }
}

void SLightPropertyEditor::SaturationTransactionBegin()
{
    auto MasterLight = ToolData->GetMasterLight();
    if (MasterLight)
        GEditor->BeginTransaction(FText::FromString(MasterLight->Name + " Saturation"));
}

FText SLightPropertyEditor::GetSaturationValueText() const
{
    FString Res = "0%";
    if (ToolData->IsAMasterLightSelected())
    {
        Res = FString::FormatAsNumber(ToolData->SelectionMasterLight->Item->Saturation * 100.0f) + "%";
    }
    return FText::FromString(Res);
}

float SLightPropertyEditor::GetSaturationValue() const
{
    if (ToolData->IsAMasterLightSelected())
    {
        return ToolData->SelectionMasterLight->Item->Saturation;
    }
    return 0.0f;
}

void SLightPropertyEditor::OnTemperatureValueChanged(float Value)
{
    for (auto SelectedItem : ToolData->GetSelectedLights())
    {
        SelectedItem->BeginTransaction();
        SelectedItem->Item->SetTemperature(Value);
    }
}

void SLightPropertyEditor::TemperatureTransactionBegin()
{
    auto MasterLight = ToolData->GetMasterLight();
    if (MasterLight)
        GEditor->BeginTransaction(FText::FromString(MasterLight->Name + " Temperature"));
}

bool SLightPropertyEditor::TemperatureEnabled() const
{
    auto MasterLight = ToolData->GetMasterLight();
    return MasterLight && MasterLight->Type != ETreeItemType::SkyLight;
    return false;
}

void SLightPropertyEditor::OnTemperatureCheckboxChecked(ECheckBoxState NewState)
{
    auto MasterLight = ToolData->GetMasterLight();
    if (MasterLight)
        GEditor->BeginTransaction(FText::FromString(MasterLight->Name + " Use Temperature"));
    for (auto SelectedItem : ToolData->GetSelectedLights())
    {
        SelectedItem->BeginTransaction();
        SelectedItem->Item->SetUseTemperature(NewState == ECheckBoxState::Checked);
    }

    GEditor->EndTransaction();    
}

FText SLightPropertyEditor::GetTemperatureValueText() const
{
    FString Res = "0";
    if (ToolData->IsAMasterLightSelected())
    {
        Res = FString::FormatAsNumber(ToolData->SelectionMasterLight->Item->Temperature) + " Kelvin";
    }
    return FText::FromString(Res);
}

ECheckBoxState SLightPropertyEditor::GetTemperatureCheckboxChecked() const
{
    ECheckBoxState State = ECheckBoxState::Unchecked;
    if (ToolData->IsAMasterLightSelected())
    {
        State = ToolData->SelectionMasterLight->Item->bUseTemperature ? ECheckBoxState::Checked : ECheckBoxState::Unchecked;
    }
    return State;
}

float SLightPropertyEditor::GetTemperatureValue() const
{
    if (ToolData->IsAMasterLightSelected())
    {
        return ToolData->SelectionMasterLight->Item->GetTemperatureNormalized();
    }
    return 0;
}

FText SLightPropertyEditor::GetTemperaturePercentage() const
{
    FString Res = "0%";
    if (ToolData->IsAMasterLightSelected())
    {
        auto Norm = ToolData->SelectionMasterLight->Item->GetTemperatureNormalized();
        Res = FString::FormatAsNumber(Norm * 100.0f) + "%";
    }
    return FText::FromString(Res);
}

