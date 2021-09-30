#pragma once

#include "IDetailCustomization.h"
#include "Slate.h"
#include "Templates/SharedPointer.h"
//#include "AppFramework/Public/Widgets/Colors/SComplexGradient.h"

UENUM()
enum ETreeItemType
{
    Folder = 0,
    SkyLight,
    SpotLight,
    DirectionalLight,
    PointLight,
    Invalid
};

struct FTreeItem : public TSharedFromThis<FTreeItem>
{
public:
    FTreeItem(class SLightControlTool* InOwningWidget = nullptr, FString InName = "Unnamed",
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

    UPROPERTY()
        TArray<TSharedPtr<FTreeItem>> Children;

    UPROPERTY()
        TSharedPtr<FTreeItem> Parent;

    UPROPERTY()
        FString Name;

    union
    {
        class ASkyLight* SkyLight;
        class APointLight* PointLight;
        class ADirectionalLight* DirectionalLight;
        class ASpotLight* SpotLight;
    };

    TEnumAsByte<ETreeItemType> Type;

    class SLightControlTool* OwningWidget;

    TSharedPtr<SBox> TableRowBox;

private:
    bool bInRename;
    TSharedPtr<SBox> TextSlot;

};


class FTreeDropOperation : public FDragDropOperation
{
public:

    TSharedPtr<FTreeItem> DraggedItem;
};

class SLightControlTool : public SCompoundWidget
{
public:

    SLATE_BEGIN_ARGS(SLightControlTool) {}
    SLATE_END_ARGS()

    void Construct(const FArguments& Args);

    ~SLightControlTool();

    void PreDestroy();

    TSharedRef<ITableRow> AddToTree(TSharedPtr<FTreeItem> Item, const TSharedRef<STableViewBase>& OwnerTable);

    void GetChildren(TSharedPtr<FTreeItem> Item, TArray<TSharedPtr<FTreeItem>>& Children);
    void SelectionCallback(TSharedPtr<FTreeItem> Item, ESelectInfo::Type SelectType);

    FReply AddFolderToTree();

    FText TestTextGetter() const;

    void ActorSpawnedCallback(AActor* Actor);

    TSharedPtr<STreeView<TSharedPtr<FTreeItem>>> Tree;
    TArray<TSharedPtr<FTreeItem>> TreeItems;


private:

    static TArray<FColor> LinearGradient(TArray<FColor> ControlPoints, FVector2D Size = FVector2D(1.0f, 256.0f), EOrientation Orientation = Orient_Vertical);

    static TArray<FColor> HSVGradient(FVector2D Size = FVector2D(1.0f, 256.0f), EOrientation Orientation = Orient_Vertical);

    static UTexture2D* MakeGradientTexture(int X = 1, int Y = 256);

    void LoadResources();
    void GenerateTextures();

    void UpdateLightList();

    SVerticalBox::FSlot& LightHeader();

    SVerticalBox::FSlot& LightPropertyEditor();

    SVerticalBox::FSlot& GeneralLightPropertyEditor();
    SVerticalBox::FSlot& LightSceneTransformEditor();

    SHorizontalBox::FSlot& LightSpecificPropertyEditor();

    TSharedPtr<FSlateImageBrush> IntensityGradientBrush;
    TSharedPtr<FSlateImageBrush> HSVGradientBrush;
    TSharedPtr<FSlateImageBrush> SaturationGradientBrush;
    TSharedPtr<FSlateImageBrush> TemperatureGradientBrush;

    UPROPERTY()
        UTexture2D* IntensityGradientTexture;
    UPROPERTY()
        UTexture2D* HSVGradientTexture;
    UPROPERTY()
        UTexture2D* SaturationGradientTexture;
    UPROPERTY()
        UTexture2D* TemperatureGradientTexture;

    TArray<TSharedPtr<FTreeItem>> SelectedItems;

    FDelegateHandle ActorSpawnedListenerHandle;


};