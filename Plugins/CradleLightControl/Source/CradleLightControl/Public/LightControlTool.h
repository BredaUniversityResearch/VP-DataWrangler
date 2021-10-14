#pragma once

#include "Slate.h"
#include "Chaos/AABB.h"
#include "Templates/SharedPointer.h"
#include "LightTreeHierarchy.h"
#include "LightPropertyEditor.h"
#include "LightSpecificPropertyEditor.h"
#include "LightControlUndoClient.h"




enum EIconType
{
    GeneralLightOff = 0,
    GeneralLightOn,
    GeneralLightUndetermined,
    SkyLightOff,
    SkyLightOn,
    SkyLightUndetermined,
    SpotLightOff,
    SpotLightOn,
    SpotLightUndetermined,
    DirectionalLightOff,
    DirectionalLightOn,
    DirectionalLightUndetermined,
    PointLightOff,
    PointLightOn,
    PointLightUndetermined,
    FolderClosed,
    FolderOpened
};


class SLightControlTool : public SCompoundWidget
{
public:

    SLATE_BEGIN_ARGS(SLightControlTool) {}

    SLATE_ARGUMENT(TSharedPtr<SDockTab>, ToolTab);

    SLATE_END_ARGS()

    void Construct(const FArguments& Args);

    ~SLightControlTool();

    void PreDestroy();
    
    void ActorSpawnedCallback(AActor* Actor);
    void OnTreeSelectionChanged();
    void UpdateExtraLightDetailBox();
    
    bool IsAMasterLightSelected() const;
    bool IsSingleGroupSelected() const;
    bool AreMultipleLightsSelected() const;
    ULightTreeItem* GetMasterLight() const;
    ULightTreeItem* GetSingleSelectedItem() const;
    void ClearSelection();

    TWeakPtr<SLightTreeHierarchy> GetTreeWidget();
    TWeakPtr<SLightPropertyEditor> GetLightPropertyEditor();

    bool OpenFileDialog(FString Title, FString DefaultPath, uint32 Flags, FString FileTypeList, TArray<FString>& OutFilenames);
    bool SaveFileDialog(FString Title, FString DefaultPath, uint32 Flags, FString FileTypeList, TArray<FString>& OutFilenames);

    FCheckBoxStyle MakeCheckboxStyleForType(ETreeItemType IconType);

    FSlateBrush& GetIcon(EIconType Icon);

private:

    void LoadResources();
    void GenerateIcons();

    SVerticalBox::FSlot& LightHeader();
    void UpdateLightHeader();

    void OnLightHeaderCheckStateChanged(ECheckBoxState NewState);
    ECheckBoxState GetLightHeaderCheckState() const;
    FText LightHeaderExtraLightsText() const;

    void UpdateItemNameBox();
    FReply StartItemNameChange(const FGeometry&, const FPointerEvent&); // called on text doubleclick
    FText ItemNameText() const;
    void CommitNewItemName(const FText& Text, ETextCommit::Type CommitType);


    void UpdateExtraNoteBox();
    FReply StartItemNoteChange(const FGeometry&, const FPointerEvent&);
    FText ItemNoteText() const;
    void CommitNewItemNote(const FText& Text, ETextCommit::Type CommitType);

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
    TSharedRef<SWidget> GroupControlDropDownLabel(ULightTreeItem* Item);
    void GroupControlDropDownSelection(ULightTreeItem* Item, ESelectInfo::Type SelectInfoType);
    FText GroupControlDropDownDefaultLabel() const;
    FText GroupControlLightList() const;

    SHorizontalBox::FSlot& LightSpecificPropertyEditor();

    TSharedPtr<SBox> ExtraLightDetailBox;
    TSharedPtr<SBox> LightHeaderBox;
    FCheckBoxStyle LightHeaderCheckboxStyle;

    bool bItemRenameInProgress;
    bool bItemNoteChangeInProgress;

    TSharedPtr<SBox> ExtraNoteBox;
    TSharedPtr<SBox> LightHeaderNameBox;


    TSharedPtr<SDockTab> ToolTab;
    TSharedPtr<FLightControlUndoClient> UndoClient;
    TSharedPtr<SLightTreeHierarchy> TreeWidget;
    TSharedPtr<SLightPropertyEditor> LightPropertyWidget;
    TSharedPtr<SLightSpecificProperties> LightSpecificWidget;


    TMap<EIconType, FSlateBrush> Icons;

    FDelegateHandle ActorSpawnedListenerHandle;

};