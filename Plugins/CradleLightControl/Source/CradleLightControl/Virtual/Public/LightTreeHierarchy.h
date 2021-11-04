#pragma once

#include "Templates/SharedPointer.h"
#include "Chaos/AABB.h"
#include "Slate.h"
#include "DMXProtocolCommon.h"
#include "Chaos/AABB.h"
#include "Chaos/AABB.h"
#include "Chaos/AABB.h"
#include "Chaos/AABB.h"
#include "Chaos/AABB.h"
#include "Chaos/AABB.h"



#include "LightTreeHierarchy.generated.h"


class UItemHandle;
class UToolData;

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

    //ECheckBoxState IsLightEnabled() const;
    //void OnCheck(ECheckBoxState NewState);

    //
    //TSharedPtr<FJsonValue> SaveToJson();
    //enum ELoadingResult
    //{
    //    Success = 0,
    //    InvalidType,
    //    LightNotFound,
    //    EngineError,
    //    MultipleErrors
    //};

    //virtual void PostTransacted(const FTransactionObjectEvent& TransactionEvent) override;


    //void FetchDataFromLight();
    //void UpdateLightColor();
    //void UpdateLightColor(FLinearColor& Color);

    //void UpdateDMX();


    

    union
    {
        class AActor* ActorPtr;
        class ASkyLight* SkyLight;
        class APointLight* PointLight;
        class ADirectionalLight* DirectionalLight;
        class ASpotLight* SpotLight;
    };


    UPROPERTY()
        bool bCastShadows;

    UPROPERTY()
        FLightDMXProperties DMXProperties;


};

DECLARE_DELEGATE_OneParam(FUpdateItemDataDelegate, UItemHandle*)
DECLARE_DELEGATE(FItemDataVerificationDelegate);
DECLARE_DELEGATE(FTreeSelectionChangedDelegate);


class SLightTreeHierarchy : public SCompoundWidget
{
public:


    SLATE_BEGIN_ARGS(SLightTreeHierarchy)
        : _Name("Unnamed tree view")
    {}

    SLATE_ARGUMENT(class UToolData*, ToolData)

    SLATE_ARGUMENT(FString, Name)

    SLATE_ARGUMENT(FUpdateItemDataDelegate, DataUpdateDelegate)

    SLATE_ARGUMENT(FItemDataVerificationDelegate, DataVerificationDelegate)
    SLATE_ARGUMENT(float, DataVerificationInterval)

    SLATE_ARGUMENT(FTreeSelectionChangedDelegate, SelectionChangedDelegate)

    SLATE_END_ARGS()

    void Construct(const FArguments& Args);
    void PreDestroy();

    void OnActorSpawned(AActor* Actor);

    void BeginTransaction();


    TSharedRef<ITableRow> AddToTree(::UItemHandle* Item, const TSharedRef<STableViewBase>& OwnerTable);

    void GetChildren(::UItemHandle* Item, TArray<UItemHandle*>& Children);
    void SelectionCallback(UItemHandle* Item, ESelectInfo::Type SelectType);
    FReply AddFolderToTree();
    void TreeExpansionCallback(UItemHandle* Item, bool bExpanded);

    EActiveTimerReturnType VerifyLights(double, float);


    void SearchBarOnChanged(const FText& NewString);


    FText GetPresetFilename() const;


    UToolData* ToolData;

    FSlateIcon SaveIcon;
    FSlateIcon SaveAsIcon;
    FSlateIcon LoadIcon;

    FText HeaderText;


    TSharedPtr<STreeView<UItemHandle*>> Tree;
    FString SearchString;

    FUpdateItemDataDelegate DataUpdateDelegate;
    FItemDataVerificationDelegate DataVerificationDelegate;

    //class UToolData* TransactionalVariables;
    TSharedPtr<FActiveTimerHandle> LightVerificationTimer;

    FTreeSelectionChangedDelegate SelectionChangedDelegate;
};
