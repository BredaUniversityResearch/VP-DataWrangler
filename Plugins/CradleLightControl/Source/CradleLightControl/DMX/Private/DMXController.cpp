#include "DMXController.h"

#include "DMXLight.h"

void SDMXController::Construct(const FArguments& Args)
{


    ChildSlot
    [
        SNew(SSplitter)
        +SSplitter::Slot()
        .Value(0.2f)
        [
            SNew(SVerticalBox)
            +SVerticalBox::Slot()
            [
                SAssignNew(DMXList, SListView<UDMXLight*>)
                .ListItemsSource(&Items)
                .OnGenerateRow(this, &SDMXController::CreateRowForItem)
                .OnSelectionChanged(this, &SDMXController::OnSelectionChanged)
            ]
            +SVerticalBox::Slot()
            [
                SNew(SButton)
                .Text(FText::FromString("Add DMX light"))
                .OnClicked(this, &SDMXController::OnAddDMXLightButtonClicked)
            ]
        ]
        +SSplitter::Slot()
            [
                FModuleManager::GetModuleChecked<FPropertyEditorModule>("PropertyEditorModule")
            ]
    ];
}

TSharedRef<ITableRow> SDMXController::CreateRowForItem(UDMXLight* Item, const TSharedRef<STableViewBase>& OwnerTable)
{
    return SNew(STableRow<UDMXLight*>, OwnerTable)
    [
        SNew(STextBlock)
        .Text(FText::FromString(Item->Name))
    ];
}

void SDMXController::OnSelectionChanged(UDMXLight* NewSelection, ESelectInfo::Type SelectionType)
{
    SelectedItems = DMXList->GetSelectedItems();

    int Index;
    if (SelectedItems.Num() > 0 && (!MasterItem || !SelectedItems.Find(MasterItem, Index)))
    {
        MasterItem = SelectedItems[0];
    }

}

FReply SDMXController::OnAddDMXLightButtonClicked()
{
    AddItem();
    return FReply::Handled();
}

void SDMXController::AddItem()
{
    auto NewItem = NewObject<UDMXLight>();

    Items.Add(NewItem);

    DMXList->RequestListRefresh();
}

