#include "LightPropertyEditor.h"
#include "LightControlTool.h"

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
    CoreToolPtr = Args._CoreToolPtr;

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

void SLightPropertyEditor::FinishInit()
{
    TreeWidget = CoreToolPtr->GetTreeWidget();
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

            auto C = FLinearColor::MakeFromHSV8(StaticCast<uint8>(NewHue * 255.0f), 255, 255).ToFColor(false);
            auto NewGradient = LinearGradient(TArray<FColor>{
                C,
                    FColor::White
            });

            RHIUpdateTexture2D(SaturationGradientTexture->GetResource()->GetTexture2DRHI(), 0, UpdateRegion, sizeof(FColor), reinterpret_cast<uint8_t*>(NewGradient.GetData()));
        });
}

const FSlateBrush* SLightPropertyEditor::GetSaturationGradientBrush() const
{
    if (CoreToolPtr->IsAMasterLightSelected())
    {
        return SaturationGradientBrush.Get();
    }
    return DefaultSaturationGradientBrush.Get();
}

void SLightPropertyEditor::OnIntensityValueChanged(float Value)
{
    if (TreeWidget.IsValid())
        for (auto SelectedItem : TreeWidget.Pin()->LightsUnderSelection)
        {
            SelectedItem->SetLightIntensity(Value * 2010.619f);
        }
}

FText SLightPropertyEditor::GetIntensityValueText() const
{
    FString Res = "0";
    if (TreeWidget.IsValid() && CoreToolPtr->IsAMasterLightSelected())
    {
        auto Light = TreeWidget.Pin()->SelectionMasterLight;
        if (Light->Type == ETreeItemType::PointLight ||
            Light->Type == ETreeItemType::SpotLight)
        {
            Res = FString::FormatAsNumber(Light->Intensity) + " Lumen";
        }
        else
            Res = "Currently not supported";
    }
    return FText::FromString(Res);
}

float SLightPropertyEditor::GetIntensityValue() const
{
    if (TreeWidget.IsValid() && CoreToolPtr->IsAMasterLightSelected())
    {
        return TreeWidget.Pin()->SelectionMasterLight->Intensity / 2010.619f;
    }
    return 0;
}

FText SLightPropertyEditor::GetIntensityPercentage() const
{
    FString Res = "0%";
    if (TreeWidget.IsValid() && CoreToolPtr->IsAMasterLightSelected())
    {
        Res = FString::FormatAsNumber(TreeWidget.Pin()->SelectionMasterLight->Intensity / 20.10619f) + "%";
    }
    return FText::FromString(Res);
}

void SLightPropertyEditor::OnHueValueChanged(float Value)
{
    if (TreeWidget.IsValid())
        for (auto SelectedItem : TreeWidget.Pin()->LightsUnderSelection)
        {
            SelectedItem->Hue = Value * 360.0f;
            SelectedItem->UpdateLightColor();
            UpdateSaturationGradient(Value);
        }
}

FText SLightPropertyEditor::GetHueValueText() const
{
    FString Res = "0";
    if (TreeWidget.IsValid() && CoreToolPtr->IsAMasterLightSelected())
    {
        Res = FString::FormatAsNumber(TreeWidget.Pin()->SelectionMasterLight->Hue);
    }
    return FText::FromString(Res);
}

float SLightPropertyEditor::GetHueValue() const
{
    if (TreeWidget.IsValid() && CoreToolPtr->IsAMasterLightSelected())
    {
        return TreeWidget.Pin()->SelectionMasterLight->Hue / 360.0f;
    }
    return 0;
}

FText SLightPropertyEditor::GetHuePercentage() const
{
    FString Res = "0%";
    if (TreeWidget.IsValid() && CoreToolPtr->IsAMasterLightSelected())
    {
        Res = FString::FormatAsNumber(TreeWidget.Pin()->SelectionMasterLight->Hue / 3.6f) + "%";
    }
    return FText::FromString(Res);
}

void SLightPropertyEditor::OnSaturationValueChanged(float Value)
{
    if (TreeWidget.IsValid())
        for (auto SelectedItem : TreeWidget.Pin()->LightsUnderSelection)
        {
            SelectedItem->Saturation = Value;
            SelectedItem->UpdateLightColor();
        }
}

FText SLightPropertyEditor::GetSaturationValueText() const
{
    FString Res = "0%";
    if (TreeWidget.IsValid() && CoreToolPtr->IsAMasterLightSelected())
    {
        Res = FString::FormatAsNumber(TreeWidget.Pin()->SelectionMasterLight->Saturation * 100.0f) + "%";
    }
    return FText::FromString(Res);
}

float SLightPropertyEditor::GetSaturationValue() const
{
    if (TreeWidget.IsValid() && CoreToolPtr->IsAMasterLightSelected())
    {
        return TreeWidget.Pin()->SelectionMasterLight->Saturation;
    }
    return 0.0f;
}

void SLightPropertyEditor::OnTemperatureValueChanged(float Value)
{
    if (TreeWidget.IsValid())
        for (auto SelectedItem : TreeWidget.Pin()->LightsUnderSelection)
        {
            SelectedItem->SetTemperature(Value * (12000.0f - 1700.0f) + 1700.0f);
        }
}

bool SLightPropertyEditor::TemperatureEnabled() const
{
    if (TreeWidget.IsValid())
    {
        auto MasterLight = TreeWidget.Pin()->SelectionMasterLight;
        return MasterLight && MasterLight->Type != ETreeItemType::SkyLight;
    }
    return false;
}

void SLightPropertyEditor::OnTemperatureCheckboxChecked(ECheckBoxState NewState)
{
    if (TreeWidget.IsValid())
        for (auto SelectedItem : TreeWidget.Pin()->LightsUnderSelection)
        {
            SelectedItem->SetUseTemperature(NewState == ECheckBoxState::Checked);            
        }
}

FText SLightPropertyEditor::GetTemperatureValueText() const
{
    FString Res = "0";
    if (TreeWidget.IsValid() && CoreToolPtr->IsAMasterLightSelected())
    {
        Res = FString::FormatAsNumber(TreeWidget.Pin()->SelectionMasterLight->Temperature) + " Kelvin";
    }
    return FText::FromString(Res);
}

ECheckBoxState SLightPropertyEditor::GetTemperatureCheckboxChecked() const
{
    ECheckBoxState State = ECheckBoxState::Unchecked;
    if (TreeWidget.IsValid() && CoreToolPtr->IsAMasterLightSelected())
    {
        State = TreeWidget.Pin()->SelectionMasterLight->bUseTemperature ? ECheckBoxState::Checked : ECheckBoxState::Unchecked;
    }
    return State;
}

float SLightPropertyEditor::GetTemperatureValue() const
{
    if (TreeWidget.IsValid() && CoreToolPtr->IsAMasterLightSelected())
    {
        return (TreeWidget.Pin()->SelectionMasterLight->Temperature - 1700.0f) / (12000.0f - 1700.0f);
    }
    return 0;
}

FText SLightPropertyEditor::GetTemperaturePercentage() const
{
    FString Res = "0%";
    if (TreeWidget.IsValid() && CoreToolPtr->IsAMasterLightSelected())
    {
        auto Norm = (TreeWidget.Pin()->SelectionMasterLight->Temperature - 1700.0f) / (12000.0f - 1700.0f);
        Res = FString::FormatAsNumber(Norm * 100.0f) + "%";
    }
    return FText::FromString(Res);
}

