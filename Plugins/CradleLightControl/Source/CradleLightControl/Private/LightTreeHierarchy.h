#pragma once

#include "Templates/SharedPointer.h"
#include "Chaos/AABB.h"
#include "Slate.h"

UENUM()
enum ETreeItemType
{
    Folder = 0,
    Mixed = Folder,
    SkyLight,
    SpotLight,
    DirectionalLight,
    PointLight,    
    Invalid
};

struct FTreeItem : public TSharedFromThis<FTreeItem>
{
public:
    FTreeItem(class SLightTreeHierarchy* InOwningWidget = nullptr, FString InName = "Unnamed",
        TArray<TSharedPtr<FTreeItem>> InChildren = TArray<TSharedPtr<FTreeItem>>());

    ECheckBoxState IsLightEnabled() const;
    void OnCheck(ECheckBoxState NewState);

    FReply TreeDragDetected(const FGeometry& Geometry, const FPointerEvent& MouseEvent);
    FReply TreeDropDetected(const FDragDropEvent& DragDropEvent);

    void GenerateTableRow();

    static bool VerifyDragDrop(TSharedPtr<FTreeItem> Dragged, TSharedPtr<FTreeItem> Destination);
    bool HasAsIndirectChild(TSharedPtr<FTreeItem> Item);

    FReply StartRename(const FGeometry&, const FPointerEvent&);
    void EndRename(const FText& Text, ETextCommit::Type CommitType);

    TSharedPtr<FJsonValue> SaveToJson();
    bool LoadFromJson(TSharedPtr<FJsonObject> JsonObject);
    void ExpandInTree();

    void FetchDataFromLight();
    void UpdateLightColor();
    void UpdateLightColor(FLinearColor& Color);
    void SetLightIntensity(float NewValue);
    void SetUseTemperature(bool NewState);
    void SetTemperature(float NewValue);

    void GetLights(TArray<TSharedPtr<FTreeItem>>& Array);

    void UpdateFolderIcon();

    bool CheckNameAgainstSearchString(const FString& SearchString);

    int LightCount() const;

    TSharedPtr<SCheckBox> StateCheckbox;
    FCheckBoxStyle CheckBoxStyle;
    UPROPERTY()
        TArray<TSharedPtr<FTreeItem>> Children;

    UPROPERTY()
        TSharedPtr<FTreeItem> Parent;

    UPROPERTY()
        FString Name;

    FString Note;

    union
    {
        class AActor* ActorPtr;
        class ASkyLight* SkyLight;
        class APointLight* PointLight;
        class ADirectionalLight* DirectionalLight;
        class ASpotLight* SpotLight;
    };

    float Hue;
    float Saturation;
    float Intensity;

    bool bUseTemperature;
    float Temperature;

    TEnumAsByte<ETreeItemType> Type;

    class SLightTreeHierarchy* OwningWidget;

    TSharedPtr<SBox> TableRowBox;

    FTreeItem* MasterLight;

    bool bExpanded;
    bool bMatchesSearchString;

private:
    bool bInRename;
    TSharedPtr<SBox> RowNameBox;
};

class FTreeDropOperation : public FDragDropOperation
{
public:

    TSharedPtr<FTreeItem> DraggedItem;
};

class SLightTreeHierarchy : public SCompoundWidget
{
public:


    SLATE_BEGIN_ARGS(SLightTreeHierarchy){}

    SLATE_ARGUMENT(class SLightControlTool*, CoreToolPtr)

    SLATE_END_ARGS()

    void Construct(const FArguments& Args);
    void PreDestroy();

    void OnActorSpawned(AActor* Actor);


    TSharedRef<ITableRow> AddToTree(TSharedPtr<FTreeItem> Item, const TSharedRef<STableViewBase>& OwnerTable);

    void GetChildren(TSharedPtr<FTreeItem> Item, TArray<TSharedPtr<FTreeItem>>& Children);
    void SelectionCallback(TSharedPtr<FTreeItem> Item, ESelectInfo::Type SelectType);
    FReply AddFolderToTree();
    void TreeExpansionCallback(TSharedPtr<FTreeItem> Item, bool bExpanded);

    TSharedPtr<FTreeItem> AddTreeItem(bool bIsFolder = false);

    EActiveTimerReturnType VerifyLights(double, float);

    void UpdateLightList();

    void SearchBarOnChanged(const FText& NewString);

    FReply SaveStateToJSON();
    FReply LoadStateFromJSON();
    bool bCurrentlyLoading;

    SLightControlTool* CoreToolPtr;

    TSharedPtr<STreeView<TSharedPtr<FTreeItem>>> Tree;
    TArray<TSharedPtr<FTreeItem>> TreeRootItems;

    TArray<TSharedPtr<FTreeItem>> SelectedItems;
    TArray<TSharedPtr<FTreeItem>> LightsUnderSelection;
    TSharedPtr<FTreeItem> SelectionMasterLight;

    TArray<FTreeItem*> ListOfLightItems;

    TSharedPtr<FActiveTimerHandle> LightVerificationTimer;

};