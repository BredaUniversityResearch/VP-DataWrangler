#pragma once

#include "CoreMinimal.h"

#include "ToolData.generated.h"

class UBaseLight;
class UItemHandle;

DECLARE_DELEGATE(FClearSelectionDelegate);
DECLARE_DELEGATE_RetVal_TwoParams(FString, FLightJsonFileDialogDelegate, FString /*Title*/, FString /*StartDir*/);
DECLARE_DELEGATE(FOnTreeStructureChangedDelegate);
DECLARE_DELEGATE_TwoParams(FItemExpansionChangedDelegate, UItemHandle*, bool);
DECLARE_DELEGATE_OneParam(FOnMasterLightTransactedDelegate, UItemHandle*);
DECLARE_DELEGATE_OneParam(FOnToolDataLoadedDelegate, uint8 /*LoadingResult*/)

DECLARE_DELEGATE_OneParam(FMetaDataExtension, TSharedPtr<FJsonObject> /*RootJsonObject*/)

UCLASS(BlueprintType)
class CRADLELIGHTCONTROL_API UToolData : public UObject
{
    GENERATED_BODY()
public:

    UToolData();

    ~UToolData();

    UFUNCTION(BlueprintPure)
    UBaseLight* GetLightByName(FString Name);



    void PostTransacted(const FTransactionObjectEvent& TransactionEvent) override;

    TSharedPtr<STreeView<UItemHandle*>> GetWidget();

    void ClearAllData();
    UItemHandle* AddItem(bool bIsFolder = false);

    bool IsAMasterLightSelected();
    bool IsSingleGroupSelected();
    bool MultipleItemsSelected();
    bool MultipleLightsInSelection();
    UItemHandle* GetMasterLight();
    UItemHandle* GetSelectedGroup();
    UItemHandle* GetSingleSelectedItem();
    const TArray<UItemHandle*>& GetSelectedLights();
    TArray<UItemHandle*> GetSelectedItems();

    void BeginTransaction();

    FReply SaveCallBack();
    FReply SaveAsCallback();
    void SaveStateToJson(FString Path, bool bUpdatePresetPath = true);
    FReply LoadCallBack();
    void LoadStateFromJSON(FString Path, bool bUpdatePresetPath = true);

    void AutoSave();

    TSharedPtr<FJsonObject> OpenMetaDataJson();

    void SaveMetaData();
    void LoadMetaData();

    FString DataName;

    bool bCurrentlyLoading;
    FString ToolPresetPath;

    FMetaDataExtension MetaDataSaveExtension;
    FMetaDataExtension MetaDataLoadExtension;
    
    FClearSelectionDelegate ClearSelectionDelegate;
    FLightJsonFileDialogDelegate SaveFileDialog;
    FLightJsonFileDialogDelegate OpenFileDialog;

    FOnTreeStructureChangedDelegate TreeStructureChangedDelegate;
    FItemExpansionChangedDelegate ItemExpansionChangedDelegate;
    FOnMasterLightTransactedDelegate MasterLightTransactedDelegate;
    FOnToolDataLoadedDelegate OnToolDataLoaded;

    FTimerHandle AutoSaveTimer;
    //TSharedPtr<FActiveTimerHandle> AutoSaveTimer;


    UPROPERTY(NonTransactional)
        UClass* ItemClass;

    UPROPERTY()
        TArray<UItemHandle*> RootItems;

    UPROPERTY()
        TArray<UItemHandle*> ListOfTreeItems;


    UPROPERTY()
        TArray<UItemHandle*> SelectedItems;

    UPROPERTY()
        TArray<UItemHandle*> LightsUnderSelection;
    UPROPERTY()
        UItemHandle* SelectionMasterLight;

    UPROPERTY()
        TArray<UItemHandle*> ListOfLightItems;


};
