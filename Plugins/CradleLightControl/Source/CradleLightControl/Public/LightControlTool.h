#pragma once

#include "IDetailCustomization.h"
#include "Slate.h"
#include "Templates/SharedPointer.h"
//#include "AppFramework/Public/Widgets/Colors/SComplexGradient.h"

struct FTreeItem : public TSharedFromThis<FTreeItem>
{
public:
    FTreeItem(class SLightControlTool* InOwningWidget = nullptr, FString InName = "Unnamed",
        TArray<TSharedPtr<FTreeItem>> InChildren = TArray<TSharedPtr<FTreeItem>>());

    UPROPERTY()
        TArray<TSharedPtr<FTreeItem>> Children;

    UPROPERTY()
        TSharedPtr<FTreeItem> Parent;

    UPROPERTY()
        FString Name;

    class SLightControlTool* OwningWidget;

    FReply TreeDragDetected(const FGeometry& Geometry, const FPointerEvent& MouseEvent);
    FReply TreeDropDetected(const FDragDropEvent& DragDropEvent);

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

    TArray<TSharedPtr<FTreeItem>> TreeItems;
    TSharedPtr<STreeView<TSharedPtr<FTreeItem>>> Tree;

    void SelectionCallback(TSharedPtr<FTreeItem> Item, ESelectInfo::Type SelectType);

    FText TestTextGetter() const;

    void ActorSpawnedCallback(AActor* Actor);

private:

    static TArray<FColor> LinearGradient(TArray<FColor> ControlPoints, FVector2D Size = FVector2D(1.0f, 256.0f), EOrientation Orientation = Orient_Vertical);

    static TArray<FColor> HSVGradient(FVector2D Size = FVector2D(1.0f, 256.0f), EOrientation Orientation = Orient_Vertical);

    static TSharedPtr<UTexture2D> MakeGradientTexture(int X = 1, int Y = 256);

    void LoadResources();
    void GenerateTextures();

    SVerticalBox::FSlot& LightHeader();

    SVerticalBox::FSlot& LightPropertyEditor();

    SVerticalBox::FSlot& GeneralLightPropertyEditor();
    SVerticalBox::FSlot& LightSceneTransformEditor();

    SHorizontalBox::FSlot& LightSpecificPropertyEditor();

    TSharedPtr<FSlateImageBrush> IntensityGradientBrush;
    TSharedPtr<FSlateImageBrush> HSVGradientBrush;
    TSharedPtr<FSlateImageBrush> SaturationGradientBrush;
    TSharedPtr<FSlateImageBrush> TemperatureGradientBrush;

    TSharedPtr<UTexture2D> IntensityGradientTexture;
    TSharedPtr<UTexture2D> HSVGradientTexture;
    TSharedPtr<UTexture2D> SaturationGradientTexture;
    TSharedPtr<UTexture2D> TemperatureGradientTexture;

    TArray<TSharedPtr<FTreeItem>> SelectedItems;

};
//
//class SMyGradient : public SComplexGradient
//{
//    virtual int32 OnPaint(const FPaintArgs& Args, const FGeometry& AllottedGeometry, const FSlateRect& MyCullingRect, FSlateWindowElementList& OutDrawElements, int32 LayerId, const FWidgetStyle& InWidgetStyle, bool bParentEnabled) const override;
//};
