#include "LightSpecificPropertyEditor.h"
#include "LightControlTool.h"

#include "Engine/SpotLight.h"

void SLightSpecificProperties::Construct(const FArguments& Args)
{
    CoreToolPtr = Args._CoreToolPtr;

    ChildSlot
    [
        SNew(SBorder)
        [
            SAssignNew(ToolSlot, SBox)
        ]
    ];

    UpdateToolState();
}

void SLightSpecificProperties::UpdateToolState()
{
    if (!CoreToolPtr->IsAMasterLightSelected())
    {
        ClearSlot();
        return;
    }

    auto Light = CoreToolPtr->GetMasterLight();

    if (Light->Type == SpotLight)
    {
        ConstructSpotLightProperties();
    }
    else if (Light->Type == DirectionalLight)
    {
        ConstructDirectionalLightProperties();
    }
    else if (Light->Type == PointLight || Light->Type == SkyLight)
    {
        ConstructPointLightProperties();
    }
    else
        ClearSlot();
}

void SLightSpecificProperties::ClearSlot()
{
    ToolSlot->SetVisibility(EVisibility::Hidden);
    ToolSlot->SetContent(SNew(SBox));
}

void SLightSpecificProperties::OnCastShadowsStateChanged(ECheckBoxState NewState)
{
    GEditor->BeginTransaction(FText::FromString(CoreToolPtr->GetMasterLight()->Name + " Cast Shadows"));
    for (auto Light : CoreToolPtr->GetTreeWidget().Pin()->LightsUnderSelection)
    {
        Light->BeginTransaction();
        Light->SetCastShadows(NewState == ECheckBoxState::Checked);
    }
    EndTransaction();
}

ECheckBoxState SLightSpecificProperties::CastShadowsState() const
{
    if (CoreToolPtr->IsAMasterLightSelected())
    {
        auto TreeWidget = CoreToolPtr->GetTreeWidget();
        auto MasterLight = TreeWidget.Pin()->SelectionMasterLight;

        auto MasterLightCastShadows = MasterLight->bCastShadows;
        auto State = MasterLightCastShadows ? ECheckBoxState::Checked : ECheckBoxState::Unchecked;
        for (auto Light : TreeWidget.Pin()->LightsUnderSelection)
        {
            if (Light->bCastShadows != MasterLightCastShadows)
            {
                State = ECheckBoxState::Undetermined;
                break;
            }
        }

        return State;
    }
    return ECheckBoxState::Undetermined;
}

void SLightSpecificProperties::ConstructDirectionalLightProperties()
{
    SVerticalBox::FSlot* HorizontalNameSlot, * HorizontalDegreesSlot, * HorizontalPercentageSlot;
    SVerticalBox::FSlot* VerticalNameSlot, * VerticalDegreesSlot, * VerticalPercentageSlot;
    SVerticalBox::FSlot* CastShadowsSlot;
    SHorizontalBox::FSlot* CastShadowsNameSlot;

    ToolSlot->SetVisibility(EVisibility::Visible);
    ToolSlot->SetContent(
        SNew(SVerticalBox)
            +SVerticalBox::Slot()
            [
                SNew(SHorizontalBox)
                +SHorizontalBox::Slot()
                .Padding(5.0f)
                [
                    SNew(SVerticalBox)
                    +SVerticalBox::Slot()
                    .Expose(HorizontalNameSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("Horizontal"))
                    ]
                    +SVerticalBox::Slot()
                    .Expose(HorizontalDegreesSlot)
                    [
                        SNew(STextBlock)
                        .Text(this, &SLightSpecificProperties::GetHorizontalValueText)
                    ]
                    +SVerticalBox::Slot()
                    [
                        SNew(SSlider)
                        .Orientation(Orient_Vertical)
                        .OnValueChanged(this, &SLightSpecificProperties::OnHorizontalValueChanged)
                        .Value(this, &SLightSpecificProperties::GetHorizontalValue)
                        .OnMouseCaptureBegin(this, &SLightSpecificProperties::BeginHorizontalTransaction)
                        .OnMouseCaptureEnd(this, &SLightSpecificProperties::EndTransaction)
                    ]
                    + SVerticalBox::Slot()
                    .Expose(HorizontalPercentageSlot)
                    [
                        SNew(STextBlock)
                        .Text(this, &SLightSpecificProperties::GetHorizontalPercentage)

                    ]
                ]
                +SHorizontalBox::Slot()
                .Padding(5.0f)
                [
                    SNew(SVerticalBox)
                    +SVerticalBox::Slot()
                    .Expose(VerticalNameSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("Vertical"))
                    ]
                    +SVerticalBox::Slot()
                    .Expose(VerticalDegreesSlot)
                    [
                        SNew(STextBlock)
                        .Text(this, &SLightSpecificProperties::GetVerticalValueText)
                    ]
                    +SVerticalBox::Slot()
                    [
                        SNew(SSlider)
                        .Orientation(Orient_Vertical)
                        .OnValueChanged(this, &SLightSpecificProperties::OnVerticalValueChanged)
                        .Value(this, &SLightSpecificProperties::GetVerticalValue)
                        .OnMouseCaptureBegin(this, &SLightSpecificProperties::BeginVerticalTransaction)
                        .OnMouseCaptureEnd(this, &SLightSpecificProperties::EndTransaction)
                    ]
                    + SVerticalBox::Slot()
                    .Expose(VerticalPercentageSlot)
                    [
                        SNew(STextBlock)
                        .Text(this, &SLightSpecificProperties::GetVerticalPercentage)
                    ]
                ]
            ]
            + SVerticalBox::Slot()
            .Expose(CastShadowsSlot)
            [
                SNew(SHorizontalBox)
                + SHorizontalBox::Slot()
                .Expose(CastShadowsNameSlot)
                .Padding(10.0f)
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("Cast Shadows"))
                ]
                + SHorizontalBox::Slot()
                [
                    SNew(SCheckBox)
                    .OnCheckStateChanged(this, &SLightSpecificProperties::OnCastShadowsStateChanged)
                    .IsChecked(this, &SLightSpecificProperties::CastShadowsState)
                ]
            ]);


    HorizontalNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    HorizontalDegreesSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    HorizontalPercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    VerticalNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    VerticalDegreesSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    VerticalPercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    CastShadowsSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    CastShadowsNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
}

void SLightSpecificProperties::ConstructSpotLightProperties()
{
    SVerticalBox::FSlot* HorizontalNameSlot, *HorizontalDegreesSlot, *HorizontalPercentageSlot;
    SVerticalBox::FSlot* VerticalNameSlot, *VerticalDegreesSlot, *VerticalPercentageSlot;
    SVerticalBox::FSlot* OuterAngleNameSlot, *OuterAngleDegreesSlot, *OuterAnglePercentageSlot;
    SVerticalBox::FSlot* InnerAngleNameSlot, *InnerAngleCheckboxSlot, *InnerAngleDegreesSlot, *InnerAnglePercentageSlot;
    SVerticalBox::FSlot* CastShadowsSlot;
    SHorizontalBox::FSlot* CastShadowsNameSlot;
    ToolSlot->SetVisibility(EVisibility::Visible);
    ToolSlot->SetContent(
        SNew(SVerticalBox)
            +SVerticalBox::Slot()
            [
                SNew(SHorizontalBox)
                +SHorizontalBox::Slot()
                .Padding(5.0f)
                [
                    SNew(SVerticalBox)
                    +SVerticalBox::Slot()
                    .Expose(HorizontalNameSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("Horizontal"))
                    ]
                    +SVerticalBox::Slot()
                    .Expose(HorizontalDegreesSlot)
                    [
                        SNew(STextBlock)
                        .Text(this, &SLightSpecificProperties::GetHorizontalValueText)
                    ]
                    +SVerticalBox::Slot()
                    [
                        SNew(SSlider)
                        .Orientation(Orient_Vertical)
                        .OnValueChanged(this, &SLightSpecificProperties::OnHorizontalValueChanged)
                        .Value(this, &SLightSpecificProperties::GetHorizontalValue)
                        .OnMouseCaptureBegin(this, &SLightSpecificProperties::BeginHorizontalTransaction)
                        .OnMouseCaptureEnd(this, &SLightSpecificProperties::EndTransaction)
                    ]
                    + SVerticalBox::Slot()
                    .Expose(HorizontalPercentageSlot)
                    [
                        SNew(STextBlock)
                        .Text(this, &SLightSpecificProperties::GetHorizontalPercentage)
                    ]
                ]
                +SHorizontalBox::Slot()
                .Padding(5.0f)
                [
                    SNew(SVerticalBox)
                    +SVerticalBox::Slot()
                    .Expose(VerticalNameSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("Vertical"))
                    ]
                    +SVerticalBox::Slot()
                    .Expose(VerticalDegreesSlot)
                    [
                        SNew(STextBlock)
                        .Text(this, &SLightSpecificProperties::GetVerticalValueText)
                    ]
                    +SVerticalBox::Slot()
                    [
                        SNew(SSlider)
                        .Orientation(Orient_Vertical)
                        .OnValueChanged(this, &SLightSpecificProperties::OnVerticalValueChanged)
                        .Value(this, &SLightSpecificProperties::GetVerticalValue)
                        .OnMouseCaptureBegin(this, &SLightSpecificProperties::BeginVerticalTransaction)
                        .OnMouseCaptureEnd(this, &SLightSpecificProperties::EndTransaction)
                    ]
                    + SVerticalBox::Slot()
                    .Expose(VerticalPercentageSlot)
                    [
                        SNew(STextBlock)
                        .Text(this, &SLightSpecificProperties::GetVerticalPercentage)
                    ]
                ]
                +SHorizontalBox::Slot()
                .Padding(5.0f)
                [
                    SNew(SVerticalBox)
                    +SVerticalBox::Slot()
                    .Expose(OuterAngleNameSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("Outer Angle"))
                    ]
                    +SVerticalBox::Slot()
                    .Expose(OuterAngleDegreesSlot)
                    [
                        SNew(STextBlock)
                        .Text(this, &SLightSpecificProperties::GetOuterAngleValueText)
                    ]
                    +SVerticalBox::Slot()
                    [
                        SNew(SSlider)
                        .Orientation(Orient_Vertical)
                        .OnValueChanged(this, &SLightSpecificProperties::OnOuterAngleValueChanged)
                        .Value(this, &SLightSpecificProperties::GetOuterAngleValue)
                        .OnMouseCaptureBegin(this, &SLightSpecificProperties::BeginOuterAngleTransaction)
                        .OnMouseCaptureEnd(this, &SLightSpecificProperties::EndTransaction)
                    ]
                    + SVerticalBox::Slot()
                    .Expose(OuterAnglePercentageSlot)
                    [
                        SNew(STextBlock)
                        .Text(this, &SLightSpecificProperties::GetOuterAnglePercentage)
                    ]
                ]
                + SHorizontalBox::Slot()
                    .Padding(5.0f)
                    [
                        SNew(SVerticalBox)
                        + SVerticalBox::Slot()
                    .Expose(InnerAngleNameSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("Inner Angle"))
                    ]
                + SVerticalBox::Slot()
                    .Expose(InnerAngleDegreesSlot)
                    [
                        SNew(STextBlock)
                        .Text(this, &SLightSpecificProperties::GetInnerAngleValueText)
                    ]
                + SVerticalBox::Slot()
                    .Expose(InnerAngleCheckboxSlot)
                    [
                        SNew(SCheckBox)
                        .ToolTipText(FText::FromString("Should the inner angle change proportionally to the outer angle?"))
                        .IsChecked(this, &SLightSpecificProperties::InnerAngleLockedState)
                        .OnCheckStateChanged(this, &SLightSpecificProperties::OnInnerAngleLockedStateChanged)
                    ]
                + SVerticalBox::Slot()
                    [
                        SNew(SSlider)
                        .Orientation(Orient_Vertical)
                        .OnValueChanged(this, &SLightSpecificProperties::OnInnerAngleValueChanged)
                        .Value(this, &SLightSpecificProperties::GetInnerAngleValue)
                        .OnMouseCaptureBegin(this, &SLightSpecificProperties::BeginInnerAngleTransaction)
                        .OnMouseCaptureEnd(this, &SLightSpecificProperties::EndTransaction)
                    ]
                + SVerticalBox::Slot()
                    .Expose(InnerAnglePercentageSlot)
                    [
                        SNew(STextBlock)
                        .Text(this, &SLightSpecificProperties::GetInnerAnglePercentage)
                    ]
                    ]
            ]
            +SVerticalBox::Slot()
            .Expose(CastShadowsSlot)
            [
                SNew(SHorizontalBox)
                +SHorizontalBox::Slot()
                .Expose(CastShadowsNameSlot)
                .Padding(10.0f)
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("Cast Shadows"))
                ]
                +SHorizontalBox::Slot()
                [
                    SNew(SCheckBox)
                    .OnCheckStateChanged(this, &SLightSpecificProperties::OnCastShadowsStateChanged)
                    .IsChecked(this, &SLightSpecificProperties::CastShadowsState)
                ]
            ]

    );

    HorizontalNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    HorizontalDegreesSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    HorizontalPercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    VerticalNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    VerticalDegreesSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    VerticalPercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    OuterAngleNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    OuterAngleDegreesSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    OuterAnglePercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    InnerAngleNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    InnerAngleDegreesSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    InnerAngleCheckboxSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    InnerAnglePercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    CastShadowsSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    CastShadowsNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
}

void SLightSpecificProperties::ConstructPointLightProperties()
{
    SVerticalBox::FSlot* CastShadowsSlot;
    SHorizontalBox::FSlot* CastShadowsNameSlot;
    ToolSlot->SetVisibility(EVisibility::Visible);
    ToolSlot->SetContent(
        SNew(SVerticalBox)
        +SVerticalBox::Slot()
        .Expose(CastShadowsSlot)
        [
            SNew(SHorizontalBox)
            + SHorizontalBox::Slot()
            .Expose(CastShadowsNameSlot)
            .Padding(10.0f)
            [
                SNew(STextBlock)
                .Text(FText::FromString("Cast Shadows"))
            ]
            + SHorizontalBox::Slot()
            [
                SNew(SCheckBox)
                .OnCheckStateChanged(this, &SLightSpecificProperties::OnCastShadowsStateChanged)
                .IsChecked(this, &SLightSpecificProperties::CastShadowsState)
            ]
        ]);
        

    CastShadowsSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    CastShadowsNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
}

void SLightSpecificProperties::EndTransaction()
{
    GEditor->EndTransaction();
}

void SLightSpecificProperties::OnHorizontalValueChanged(float NormalizedValue)
{
    auto Light = CoreToolPtr->GetMasterLight();
    auto Delta = ((NormalizedValue - 0.5f) * 360.0f) - Light->Horizontal;

    for (auto SelectedLight : CoreToolPtr->GetTreeWidget().Pin()->LightsUnderSelection)
    {
        SelectedLight->BeginTransaction();
        SelectedLight->AddHorizontal(Delta);
    }    
}

void SLightSpecificProperties::BeginHorizontalTransaction()
{
    GEditor->BeginTransaction(FText::FromString(CoreToolPtr->GetMasterLight()->Name + " Horizontal Rotation"));
}

FText SLightSpecificProperties::GetHorizontalValueText() const
{
    auto Light = CoreToolPtr->GetMasterLight();

    return FText::FromString(FString::Printf(TEXT("%.0f"), Light->Horizontal));
}

float SLightSpecificProperties::GetHorizontalValue() const
{
    auto Light = CoreToolPtr->GetMasterLight();

    return Light->Horizontal / 360.0f + 0.5f;
}

FText SLightSpecificProperties::GetHorizontalPercentage() const
{
    auto Light = CoreToolPtr->GetMasterLight();

    auto Euler = Light->SpotLight->GetActorRotation().Euler();

    return FText::FromString(FString::FormatAsNumber(Light->Horizontal / 3.6f + 50.0f) + "%");
}

void SLightSpecificProperties::OnVerticalValueChanged(float NormalizedValue)
{
    auto Light = CoreToolPtr->GetMasterLight();
    auto Delta = ((NormalizedValue - 0.5f) * 360.0f) - Light->Vertical;

    for (auto SelectedLight : CoreToolPtr->GetTreeWidget().Pin()->LightsUnderSelection)
    {
        SelectedLight->BeginTransaction();
        SelectedLight->AddVertical(Delta);
    }
}

void SLightSpecificProperties::BeginVerticalTransaction()
{
    GEditor->BeginTransaction(FText::FromString(CoreToolPtr->GetMasterLight()->Name + " Vertical Rotation"));

}


FText SLightSpecificProperties::GetVerticalValueText() const
{
    auto Light = CoreToolPtr->GetMasterLight();

    return FText::FromString(FString::Printf(TEXT("%.0f"), Light->Vertical));
}

float SLightSpecificProperties::GetVerticalValue() const
{
    auto Light = CoreToolPtr->GetMasterLight();

    return Light->Vertical / 360.0f + 0.5f;
}

FText SLightSpecificProperties::GetVerticalPercentage() const
{
    auto Light = CoreToolPtr->GetMasterLight();

    auto Euler = Light->SpotLight->GetActorRotation().Euler();

    return FText::FromString(FString::FormatAsNumber(Light->Vertical / 3.6f + 50.0f) + "%");
}

void SLightSpecificProperties::OnInnerAngleValueChanged(float NormalizedValue)
{
    auto Light = CoreToolPtr->GetMasterLight();
    auto Angle = NormalizedValue * 80.0f;

    for (auto SelectedLight : CoreToolPtr->GetTreeWidget().Pin()->LightsUnderSelection)
    {
        SelectedLight->BeginTransaction();
        SelectedLight->SetInnerConeAngle(Angle);
    }
}

void SLightSpecificProperties::BeginInnerAngleTransaction()
{
    GEditor->BeginTransaction(FText::FromString(CoreToolPtr->GetMasterLight()->Name + " Inner Angle"));
}

void SLightSpecificProperties::OnInnerAngleLockedStateChanged(ECheckBoxState NewState)
{
    if (CoreToolPtr->GetTreeWidget().IsValid())
    {
        GEditor->BeginTransaction(FText::FromString(CoreToolPtr->GetMasterLight()->Name + " Inner Angle Lock"));
        for (auto Light : CoreToolPtr->GetTreeWidget().Pin()->LightsUnderSelection)
        {
            Light->BeginTransaction(false);
            Light->bLockInnerAngleToOuterAngle = NewState == ECheckBoxState::Checked;
        }
        EndTransaction();
    }
}

ECheckBoxState SLightSpecificProperties::InnerAngleLockedState() const
{
    auto Light = CoreToolPtr->GetMasterLight();
    if (Light)
    {
        return Light->bLockInnerAngleToOuterAngle ? ECheckBoxState::Checked : ECheckBoxState::Unchecked;
    }
    return ECheckBoxState::Undetermined;
}

FText SLightSpecificProperties::GetInnerAngleValueText() const
{
    auto Light = CoreToolPtr->GetMasterLight();

    return FText::FromString(FString::Printf(TEXT("%.0f"), Light->InnerAngle));
}

float SLightSpecificProperties::GetInnerAngleValue() const
{
    auto Light = CoreToolPtr->GetMasterLight();

    return Light->InnerAngle / 80.0f;
}

FText SLightSpecificProperties::GetInnerAnglePercentage() const
{
    auto Light = CoreToolPtr->GetMasterLight();

    auto Euler = Light->SpotLight->GetActorRotation().Euler();

    return FText::FromString(FString::FormatAsNumber(Light->InnerAngle / 0.8f) + "%");
}


void SLightSpecificProperties::OnOuterAngleValueChanged(float NormalizedValue)
{
    auto Light = CoreToolPtr->GetMasterLight();
    auto Angle = NormalizedValue * 80.0f;
    for (auto SelectedLight : CoreToolPtr->GetTreeWidget().Pin()->LightsUnderSelection)
    {
        SelectedLight->SetOuterConeAngle(Angle);
        SelectedLight->BeginTransaction();
    }
}

void SLightSpecificProperties::BeginOuterAngleTransaction()
{
    GEditor->BeginTransaction(FText::FromString(CoreToolPtr->GetMasterLight()->Name + " Outer Angle"));
}

FText SLightSpecificProperties::GetOuterAngleValueText() const
{
    auto Light = CoreToolPtr->GetMasterLight();

    return FText::FromString(FString::Printf(TEXT("%.0f"), Light->OuterAngle));
}

float SLightSpecificProperties::GetOuterAngleValue() const
{
    auto Light = CoreToolPtr->GetMasterLight();

    return Light->OuterAngle / 80.0f;
}

FText SLightSpecificProperties::GetOuterAnglePercentage() const
{
    auto Light = CoreToolPtr->GetMasterLight();

    auto Euler = Light->SpotLight->GetActorRotation().Euler();

    return FText::FromString(FString::FormatAsNumber(Light->OuterAngle / 0.8f) + "%");
}
