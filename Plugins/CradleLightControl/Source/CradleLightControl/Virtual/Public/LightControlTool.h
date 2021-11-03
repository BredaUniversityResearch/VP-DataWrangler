#pragma once

#include "ItemHandle.h"
#include "Slate.h"
#include "Templates/SharedPointer.h"
#include "LightTreeHierarchy.h"
#include "LightPropertyEditor.h"
#include "LightSpecificPropertyEditor.h"
#include "LightItemHeader.h"


class UToolData;

DECLARE_DELEGATE_RetVal_SixParams(bool,  FFileDialogDelegate, FString, void*, FString, uint32, FString, TArray<FString>&);

class SLightControlTool : public SCompoundWidget
{
public:

    SLATE_BEGIN_ARGS(SLightControlTool) {}

    SLATE_ARGUMENT(TSharedPtr<SDockTab>, ToolTab);
    SLATE_ARGUMENT(FFileDialogDelegate, OpenFileDialogDelegate);
    SLATE_ARGUMENT(FFileDialogDelegate, SaveFileDialogDelegate);

    SLATE_END_ARGS()

    void Construct(const FArguments& Args);

    ~SLightControlTool();

    void PreDestroy();
    
    void ActorSpawnedCallback(AActor* Actor);
    void OnTreeSelectionChanged();
    void UpdateExtraLightDetailBox();
    
    void ClearSelection();

    TWeakPtr<SLightTreeHierarchy> GetTreeWidget();
    TWeakPtr<SLightPropertyEditor> GetLightPropertyEditor();

    FString OpenFileDialog(FString Title, FString StartingPath);
    FString SaveFileDialog(FString Title, FString StartingPath);

    //FSlateBrush& GetIcon(EIconType Icon);

    void UpdateLightList();


    static void UpdateItemData(UItemHandle* ItemHandle);
    void VerifyTreeData();

private:

    void LoadResources();

    SVerticalBox::FSlot& LightHeader();
     

    SVerticalBox::FSlot& LightPropertyEditor();


    TSharedRef<SBox> LightTransformViewer();
    FReply SelectItemInScene();
    FReply SelectItemParent();
    bool SelectItemParentButtonEnable() const;
    FText GetItemParentName() const;
    FText GetItemPosition() const;
    FText GetItemRotation() const;
    FText GetItemScale() const;

    TSharedRef<SBox> GroupControls();
    TSharedRef<SWidget> GroupControlDropDownLabel(UItemHandle* Item);
    void GroupControlDropDownSelection(UItemHandle* Item, ESelectInfo::Type SelectInfoType);
    FText GroupControlDropDownDefaultLabel() const;
    FText GroupControlLightList() const;

    SHorizontalBox::FSlot& LightSpecificPropertyEditor();

    TSharedPtr<SBox> ExtraLightDetailBox;

    FFileDialogDelegate OpenFileDialogDelegate;
    FFileDialogDelegate SaveFileDialogDelegate;


    TSharedPtr<SDockTab> ToolTab;
    TSharedPtr<SLightTreeHierarchy> TreeWidget;
    TSharedPtr<SLightPropertyEditor> LightPropertyWidget;
    TSharedPtr<SLightSpecificProperties> LightSpecificWidget;
    TSharedPtr<SLightItemHeader> ItemHeader;
    UToolData* ToolData;
    TSharedPtr<FActiveTimerHandle> DataAutoSaveTimer;

    FDelegateHandle ActorSpawnedListenerHandle;

};