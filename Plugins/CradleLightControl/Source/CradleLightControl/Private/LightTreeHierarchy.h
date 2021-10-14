#pragma once

#include "Templates/SharedPointer.h"
#include "Chaos/AABB.h"
#include "Slate.h"
#include "Chaos/AABB.h"
#include "Chaos/AABB.h"
#include "Chaos/AABB.h"
#include "Chaos/AABB.h"
#include "Chaos/AABB.h"
#include "Chaos/AABB.h"
#include "Chaos/AABB.h"
#include "Chaos/AABB.h"
#include "Chaos/AABB.h"
#include "Chaos/AABB.h"
#include "Chaos/AABB.h"
#include "Chaos/AABB.h"


#include "LightTreeHierarchy.generated.h"

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
UCLASS()
class ULightTreeItem : public UObject
{
    GENERATED_BODY()
public:
    ULightTreeItem()
        : ULightTreeItem(nullptr) {}
    ULightTreeItem(class SLightTreeHierarchy* InOwningWidget, FString InName = "Unnamed",
        TArray<ULightTreeItem*> InChildren = TArray<ULightTreeItem*>());

    ECheckBoxState IsLightEnabled() const;
    void OnCheck(ECheckBoxState NewState);

    FReply TreeDragDetected(const FGeometry& Geometry, const FPointerEvent& MouseEvent);
    FReply TreeDropDetected(const FDragDropEvent& DragDropEvent);

    void GenerateTableRow();

    static bool VerifyDragDrop(ULightTreeItem* Dragged, ULightTreeItem* Destination);
    bool HasAsIndirectChild(ULightTreeItem* Item);

    FReply StartRename(const FGeometry&, const FPointerEvent&);
    void EndRename(const FText& Text, ETextCommit::Type CommitType);

    TSharedPtr<FJsonValue> SaveToJson();
    enum ELoadingResult
    {
        Success = 0,
        InvalidType,
        LightNotFound,
        EngineError,
        MultipleErrors
    };

    virtual void PostEditUndo(TSharedPtr<ITransactionObjectAnnotation> TransactionAnnotation) override;
    virtual void PostTransacted(const FTransactionObjectEvent& TransactionEvent) override;

    ELoadingResult LoadFromJson(TSharedPtr<FJsonObject> JsonObject);
    void ExpandInTree();

    void FetchDataFromLight();
    void UpdateLightColor();
    void UpdateLightColor(FLinearColor& Color);
    void SetLightIntensity(float NewValue);
    void SetUseTemperature(bool NewState);
    void SetTemperature(float NewValue);

    void SetCastShadows(bool bState);

    void AddHorizontal(float Degrees);
    void AddVertical(float Degrees);
    void SetInnerConeAngle(float NewValue);
    void SetOuterConeAngle(float NewValue);

    void GetLights(TArray<ULightTreeItem*>& Array);

    void UpdateFolderIcon();

    bool CheckNameAgainstSearchString(const FString& SearchString);

    int LightCount() const;

    void BeginTransaction(bool bContinueRecursively = true);

    TSharedPtr<SCheckBox> StateCheckbox;
    FCheckBoxStyle CheckBoxStyle;
    UPROPERTY()
        TArray<ULightTreeItem*> Children;

    UPROPERTY()
        ULightTreeItem* Parent;

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
    UPROPERTY()
        float Hue;
    UPROPERTY()
        float Saturation;
    UPROPERTY()
        float Intensity;

    UPROPERTY()
        bool bUseTemperature;
    UPROPERTY()
    float Temperature;

    UPROPERTY()
        float Horizontal;
    UPROPERTY()
        float Vertical;
    UPROPERTY()
        float InnerAngle;
    UPROPERTY()
        float OuterAngle;
    UPROPERTY()
        bool bLockInnerAngleToOuterAngle;

    UPROPERTY()
        bool bCastShadows;

    TEnumAsByte<ETreeItemType> Type;

    class SLightTreeHierarchy* OwningWidget;

    TSharedPtr<SBox> TableRowBox;

    ULightTreeItem* MasterLight;

    bool bExpanded;
    bool bMatchesSearchString;

private:
    bool bInRename;
    TSharedPtr<SBox> RowNameBox;
};

class FTreeDropOperation : public FDragDropOperation
{
public:

    ULightTreeItem* DraggedItem;
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


    TSharedRef<ITableRow> AddToTree(::ULightTreeItem* Item, const TSharedRef<STableViewBase>& OwnerTable);

    void GetChildren(::ULightTreeItem* Item, TArray<ULightTreeItem*>& Children);
    void SelectionCallback(ULightTreeItem* Item, ESelectInfo::Type SelectType);
    FReply AddFolderToTree();
    void TreeExpansionCallback(ULightTreeItem* Item, bool bExpanded);

    ULightTreeItem* AddTreeItem(bool bIsFolder = false);

    EActiveTimerReturnType VerifyLights(double, float);

    void UpdateLightList();

    void SearchBarOnChanged(const FText& NewString);

    EActiveTimerReturnType AutoSave(double, float);

    FReply SaveCallBack();
    FReply SaveAsCallback();
    void SaveStateToJson(FString Path, bool bUpdatePresetPath = true);
    FReply LoadCallBack();
    void LoadStateFromJSON(FString Path, bool bUpdatePresetPath = true);

    void SaveMetaData();
    void LoadMetaData();

    FText GetPresetFilename() const;

    bool bCurrentlyLoading;
    FString ToolPresetPath;
    FSlateIcon SaveIcon;
    FSlateIcon SaveAsIcon;
    FSlateIcon LoadIcon;

    SLightControlTool* CoreToolPtr;

    TSharedPtr<STreeView<ULightTreeItem*>> Tree;
    UPROPERTY()
    TArray<ULightTreeItem*> TreeRootItems;

    TArray<ULightTreeItem*> SelectedItems;
    TArray<ULightTreeItem*> LightsUnderSelection;
    ULightTreeItem* SelectionMasterLight;

    UPROPERTY()
    TArray<ULightTreeItem*> ListOfTreeItems;
    TArray<ULightTreeItem*> ListOfLightItems;

    TSharedPtr<FActiveTimerHandle> LightVerificationTimer;
    TSharedPtr<FActiveTimerHandle> AutoSaveTimer;
};