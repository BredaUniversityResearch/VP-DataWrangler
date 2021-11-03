#include "ItemHeader.h"

#include "ToolData.h"
#include "ItemHandle.h"
#include "BaseLight.h"
void SItemHeader::Construct(const FArguments& Args)
{
    check(Args._ToolData);
    ToolData = Args._ToolData;
    ChildSlot
        .HAlign(HAlign_Fill)
        [
            SAssignNew(ContentBox, SBox)
        ];

    Update();

}

void SItemHeader::Update()
{
    if (ToolData->IsAMasterLightSelected() || ToolData->IsSingleGroupSelected())
    {
        SHorizontalBox::FSlot* NameSlot;
        SHorizontalBox::FSlot* CheckboxSlot;
        SVerticalBox::FSlot* TopSlot;
        TEnumAsByte<ETreeItemType> IconType;
        if (ToolData->IsSingleGroupSelected())
        {
            IconType = ToolData->GetSelectedGroup()->Type;
        }
        else
        {
            IconType = ToolData->GetMasterLight()->Type;
        }
        for (auto Light : ToolData->GetSelectedLights())
        {
            if (IconType != Light->Type)
            {
                IconType = Mixed;
                break;
            }
        }
        LightHeaderCheckboxStyle = ToolData->MakeCheckboxStyleForType(IconType);

        ContentBox->SetHAlign(HAlign_Fill);
        ContentBox->SetPadding(FMargin(5.0f, 0.0f));
        ContentBox->SetContent(
            SNew(SVerticalBox)
            + SVerticalBox::Slot()
            .Expose(TopSlot)
            .Padding(0.0f, 0.0f, 0.0f, 5.0f)
            [
                SNew(SHorizontalBox)
                + SHorizontalBox::Slot()
            .HAlign(HAlign_Fill)
            .Expose(NameSlot)
            [
                SAssignNew(LightHeaderNameBox, SBox)
                // SNew(STextBlock)
                // .Text(this, &SLightControlTool::LightHeaderText)
                // .Font(FSlateFontInfo(FCoreStyle::GetDefaultFont(), 18))
            ]
        + SHorizontalBox::Slot()
            .HAlign(HAlign_Right)
            .Padding(0.0f, 0.0f, 15.0f, 0.0f)
            .Expose(CheckboxSlot)
            [
                SNew(SCheckBox)
                .Style(&LightHeaderCheckboxStyle)
            .OnCheckStateChanged(this, &SItemHeader::OnLightHeaderCheckStateChanged)
            .IsChecked(this, &SItemHeader::GetLightHeaderCheckState)
            .RenderTransform(FSlateRenderTransform(1.2f))
            ]
            ]
        + SVerticalBox::Slot()
            .HAlign(HAlign_Fill)
            .VAlign(VAlign_Bottom)
            .Padding(0.0f, 3.0f, 30.0f, 3.0f)
            [
                SAssignNew(ExtraNoteBox, SBox)
            ]
        + SVerticalBox::Slot()
            .Padding(0.0f, 5.0f, 0.0f, 0.0f)
            [
                SNew(STextBlock)
                .Text(this, &SItemHeader::LightHeaderExtraLightsText)
            .Visibility(ToolData->IsSingleGroupSelected()
                || ToolData->MultipleLightsInSelection() ?
                EVisibility::Visible : EVisibility::Collapsed)
            ]
        );
        bItemNoteChangeInProgress = false;
        bItemRenameInProgress = false;
        UpdateItemNameBox();
        UpdateExtraNoteBox();

        CheckboxSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
        TopSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    }
    else
    {
        ContentBox->SetContent(
            SNew(STextBlock)
            .Text(FText::FromString("No lights currently selected"))
            .Font(FSlateFontInfo(FCoreStyle::GetDefaultFont(), 18)));
    }
}


void SItemHeader::OnLightHeaderCheckStateChanged(ECheckBoxState NewState)
{
    if (ToolData->IsAMasterLightSelected())
    {
        GEditor->BeginTransaction(FText::FromString(ToolData->GetMasterLight()->Name + " State Changed"));
        for (auto Light : ToolData->GetSelectedLights())
        {
            Light->Item->SetEnabled(NewState == ECheckBoxState::Checked); // Use the callback used by the tree to modify the state
        }
        GEditor->EndTransaction();
    }
}


ECheckBoxState SItemHeader::GetLightHeaderCheckState() const
{
    if (ToolData->IsAMasterLightSelected())
    {
        return ToolData->GetMasterLight()->Item->IsEnabled() ? ECheckBoxState::Checked : ECheckBoxState::Unchecked;
    }
    return ECheckBoxState::Undetermined;
}

FText SItemHeader::LightHeaderExtraLightsText() const
{
    if (ToolData->MultipleItemsSelected())
    {
        int GroupCount = 0;
        int LightCount = 0;
        int TotalLightCount = 0;
        for (auto SelectedItem : ToolData->SelectedItems)
        {
            if (SelectedItem->Type == Folder)
            {
                GroupCount++;
            }
            else
            {
                LightCount++;
            }
            TotalLightCount += SelectedItem->LightCount();
        }

        return FText::FromString(FString::Printf(TEXT("%d Groups and %d Lights selected (Total %d lights affected)"), GroupCount, LightCount, TotalLightCount));
    }
    return FText::FromString("");
}

void SItemHeader::UpdateItemNameBox()
{
    auto Item = ToolData->GetSingleSelectedItem();
    if (Item)
    {
        if (bItemRenameInProgress)
        {
            LightHeaderNameBox->SetContent(
                SNew(SEditableText)
                .Text(this, &SItemHeader::ItemNameText)
                .OnTextCommitted(this, &SItemHeader::CommitNewItemName)
                .SelectAllTextWhenFocused(true)
                .Font(FSlateFontInfo(FCoreStyle::GetDefaultFont(), 18))
            );
        }
        else
        {
            LightHeaderNameBox->SetContent(
                SNew(STextBlock)
                .Text(this, &SItemHeader::ItemNameText)
                .OnDoubleClicked(this, &SItemHeader::StartItemNameChange)
                .Font(FSlateFontInfo(FCoreStyle::GetDefaultFont(), 18))
            );
        }
    }
}

FReply SItemHeader::StartItemNameChange(const FGeometry&, const FPointerEvent&)
{
    bItemRenameInProgress = true;
    UpdateItemNameBox();
    return FReply::Handled();
}

FText SItemHeader::ItemNameText() const
{
    if (ToolData->IsSingleGroupSelected())
    {
        return FText::FromString(ToolData->GetSelectedGroup()->Name);

    }
    return FText::FromString(ToolData->GetMasterLight()->Name);
}

void SItemHeader::CommitNewItemName(const FText& Text, ETextCommit::Type CommitType)
{
    if (CommitType == ETextCommit::OnEnter && !Text.IsEmpty())
    {
        auto Item = ToolData->GetSingleSelectedItem();
        GEditor->BeginTransaction(FText::FromString(Item->Name + " Rename"));
        Item->BeginTransaction();

        Item->Name = Text.ToString();
        Item->GenerateTableRow();

        GEditor->EndTransaction();
    }
    bItemRenameInProgress = false;
}

void SItemHeader::UpdateExtraNoteBox()
{
    ExtraNoteBox->SetVisibility(EVisibility::Visible);
    if (ToolData->IsSingleGroupSelected())
    {
        ExtraNoteBox->SetContent(
            SNew(STextBlock)
            .Text(FText::FromString("Group")));
    }
    else if (ToolData->IsAMasterLightSelected())
    {
        auto MasterLight = ToolData->GetMasterLight();
        if (MasterLight->Note.IsEmpty() && !bItemNoteChangeInProgress)
        {
            ExtraNoteBox->SetContent(
                SNew(STextBlock)
                .Text(FText::FromString("+ Add note"))
                .OnDoubleClicked(this, &SItemHeader::StartItemNoteChange)
            );
        }
        else if (bItemNoteChangeInProgress)
        {
            ExtraNoteBox->SetContent(
                SNew(SEditableText)
                .Text(this, &SItemHeader::ItemNoteText)
                .OnTextCommitted(this, &SItemHeader::CommitNewItemNote)
            );
        }
        else
        {
            ExtraNoteBox->SetContent(
                SNew(STextBlock)
                .Text(this, &SItemHeader::ItemNoteText)
                .OnDoubleClicked(this, &SItemHeader::StartItemNoteChange)
            );
        }
    }
    else
        ExtraNoteBox->SetVisibility(EVisibility::Collapsed);
}

FReply SItemHeader::StartItemNoteChange(const FGeometry&, const FPointerEvent&)
{
    bItemNoteChangeInProgress = true;
    UpdateExtraNoteBox();
    return FReply::Handled();
}

FText SItemHeader::ItemNoteText() const
{
    auto Item = ToolData->GetMasterLight();
    if (Item->Note.IsEmpty())
    {
        return FText::FromString("Note");
    }
    return FText::FromString(ToolData->GetMasterLight()->Note);
}

void SItemHeader::CommitNewItemNote(const FText& Text, ETextCommit::Type CommitType)
{
    if (CommitType == ETextCommit::OnEnter)
    {
        auto Item = ToolData->GetMasterLight();
        GEditor->BeginTransaction(FText::FromString(Item->Name + " Note changed"));
        Item->BeginTransaction();

        Item->Note = Text.ToString();
        Item->GenerateTableRow();

        GEditor->EndTransaction();
    }
    bItemNoteChangeInProgress = false;
    UpdateExtraNoteBox();
}

