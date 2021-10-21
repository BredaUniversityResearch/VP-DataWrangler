#pragma once

#include "Slate.h"

class UDMXLight;
class SDMXController : public SCompoundWidget
{


    SLATE_BEGIN_ARGS(SDMXController){}



    SLATE_END_ARGS()
public:
    void Construct(const FArguments& Args);

private:

    TSharedRef<ITableRow> CreateRowForItem(UDMXLight* Item, const TSharedRef<STableViewBase>& OwnerTable);
    void OnSelectionChanged(UDMXLight* NewSelection, ESelectInfo::Type SelectionType);

    FReply OnAddDMXLightButtonClicked();

    void AddItem();

    TSharedPtr<SListView<UDMXLight*>> DMXList;

    TArray<UDMXLight*> Items;
    TArray<UDMXLight*> SelectedItems;
    UDMXLight* MasterItem;

};