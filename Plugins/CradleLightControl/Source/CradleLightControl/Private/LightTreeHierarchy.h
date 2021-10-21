#pragma once

#include "Templates/SharedPointer.h"
#include "Chaos/AABB.h"
#include "Slate.h"
#include "DMXProtocolCommon.h"


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

class FLightDMXNotifyHook : public FNotifyHook
{
public:
    FLightDMXNotifyHook(){};
    FLightDMXNotifyHook(struct FLightDMXProperties* InPropertiesRef)
        : PropertiesRef (InPropertiesRef)
    { }
    virtual void NotifyPostChange(const FPropertyChangedEvent& PropertyChangedEvent, FProperty* PropertyThatChanged) override;

    FLightDMXProperties* PropertiesRef;
};
//
//USTRUCT(BlueprintType)
//struct FNormalizedLightData
//{
//    bool bEnabled;
//    float Intensity;
//    float Hue;
//    float Saturation;
//    bool bUseTemperature;
//    float Temperature;
//    float Horizontal;
//    float Vertical;
//};

UCLASS(Blueprintable)
class ULightDataToDMXConversion : public UObject
{

    GENERATED_BODY()
public:
    ULightDataToDMXConversion() {};

    UFUNCTION(BlueprintImplementableEvent)
        void Convert(class ULightTreeItem* TreeItem);

    UFUNCTION(BlueprintCallable)
        void SetChannel(int32 InChannel, uint8 InValue);

    UPROPERTY()
        TMap<int32, uint8> Channels;

    int StartingChannel;

    UPROPERTY(BlueprintReadWrite)
        FString Shitter;
};

USTRUCT()
struct FLightDMXProperties
{
    GENERATED_BODY()
    FDMXOutputPortSharedPtr OutputPort;

    UPROPERTY(EditAnywhere)
    bool bUseDmx;
    UPROPERTY(EditAnywhere)
    TSubclassOf<ULightDataToDMXConversion> Conversion;

    UPROPERTY()
    ULightDataToDMXConversion* DataConverter;

    UPROPERTY(EditAnywhere)
        int StartingChannel;

    UPROPERTY()
        ULightTreeItem* Owner;
};

UCLASS(BlueprintType)
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

    virtual void PostTransacted(const FTransactionObjectEvent& TransactionEvent) override;

    ELoadingResult LoadFromJson(TSharedPtr<FJsonObject> JsonObject);
    void ExpandInTree();
    FReply RemoveFromTree();

    void FetchDataFromLight();
    void UpdateLightColor();
    void UpdateLightColor(FLinearColor& Color);

    void SetEnabled(bool bNewState);
    void SetLightIntensity(float NewValue);
    void SetUseTemperature(bool NewState);
    void SetTemperature(float NewValue);

    void SetCastShadows(bool bState);

    void UpdateDMX();

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

    UPROPERTY()
        FString Note;

    union
    {
        class AActor* ActorPtr;
        class ASkyLight* SkyLight;
        class APointLight* PointLight;
        class ADirectionalLight* DirectionalLight;
        class ASpotLight* SpotLight;
    };

    UPROPERTY(BlueprintReadOnly)
        bool bIsEnabled;
    UPROPERTY(BlueprintReadOnly)
        float Hue;
    UPROPERTY(BlueprintReadOnly)
        float Saturation;
    UPROPERTY(BlueprintReadOnly)
        float Intensity;

    UPROPERTY(BlueprintReadOnly)
        bool bUseTemperature;
    UPROPERTY(BlueprintReadOnly)
    float Temperature;

    UPROPERTY(BlueprintReadOnly)
        float Horizontal;
    UPROPERTY(BlueprintReadOnly)
        float Vertical;
    UPROPERTY(BlueprintReadOnly)
        float InnerAngle;
    UPROPERTY(BlueprintReadOnly)
        float OuterAngle;
    UPROPERTY()
        bool bLockInnerAngleToOuterAngle;

    UPROPERTY()
        bool bCastShadows;

    UPROPERTY()
        FLightDMXProperties DMXProperties;

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

UCLASS()
class UTreeTransactionalVariables : public UObject
{
    GENERATED_BODY()
public:

    UTreeTransactionalVariables()
    {
        SetFlags(GetFlags() | RF_Transactional);
    }

    void PostTransacted(const FTransactionObjectEvent& TransactionEvent) override;

    TWeakPtr<class SLightTreeHierarchy> Widget;

    UPROPERTY()
    TArray<ULightTreeItem*> RootItems;


    UPROPERTY()
        TArray<ULightTreeItem*> ListOfTreeItems;


    UPROPERTY()
    TArray<ULightTreeItem*> SelectedItems;

    UPROPERTY()
        TArray<ULightTreeItem*> LightsUnderSelection;
    UPROPERTY()
        ULightTreeItem* SelectionMasterLight;

    UPROPERTY()
        TArray<ULightTreeItem*> ListOfLightItems;
};

USTRUCT()
struct FTreeTransactionVarialblesHandle
{
    GENERATED_BODY()
public:
    UTreeTransactionalVariables* operator->() const { return Ptr; }
    UTreeTransactionalVariables* operator*() const { return Ptr; }

    
    UPROPERTY()
        UTreeTransactionalVariables* Ptr;
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

    void BeginTransaction();


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

    UTreeTransactionalVariables* TransactionalVariables;

    //UPROPERTY()
    //TArray<ULightTreeItem*> TreeRootItems;


    TSharedPtr<FActiveTimerHandle> LightVerificationTimer;
    TSharedPtr<FActiveTimerHandle> AutoSaveTimer;
};
