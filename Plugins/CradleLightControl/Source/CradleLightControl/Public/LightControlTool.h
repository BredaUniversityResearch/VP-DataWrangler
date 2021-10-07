#pragma once

#include "Slate.h"
#include "Chaos/AABB.h"
#include "Templates/SharedPointer.h"
#include "LightTreeHierarchy.h"
#include "LightPropertyEditor.h"




enum EIconType
{
    SkyLightOff = 0,
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
    GeneralLightOff,
    GeneralLightOn,
    GeneralLightUndetermined,
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
    

    FText TestTextGetter() const;

    void ActorSpawnedCallback(AActor* Actor);
    void OnTreeSelectionChanged();
    void UpdateExtraLightDetailBox();
    
    bool IsLightSelected() const;
    bool AreMultipleLightsSelected() const;

    TWeakPtr<SLightTreeHierarchy> GetTreeWidget();

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
    TSharedRef<SWidget> GroupControlDropDownLabel(TSharedPtr<FTreeItem> Item);
    void GroupControlDropDownSelection(TSharedPtr<FTreeItem> Item, ESelectInfo::Type SelectInfoType);
    FText GroupControlDropDownDefaultLabel() const;
    FText GroupControlLightList() const;

    SHorizontalBox::FSlot& LightSpecificPropertyEditor();

    TSharedPtr<SBox> ExtraLightDetailBox;
    TSharedPtr<SBox> LightHeaderBox;
    FCheckBoxStyle LightHeaderCheckboxStyle;

    TSharedPtr<SDockTab> ToolTab;
    TSharedPtr<SLightTreeHierarchy> TreeWidget;
    TSharedPtr<SLightPropertyEditor> LightPropertyWidget;


    TMap<EIconType, FSlateBrush> Icons;

    FDelegateHandle ActorSpawnedListenerHandle;

};