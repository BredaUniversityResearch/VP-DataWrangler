#include "LightControlTool.h"

#include "Slate.h"

#include "DetailCategoryBuilder.h"
#include "DetailLayoutBuilder.h"
#include "DetailWidgetRow.h"
#include "Engine/Engine.h"
#include "Interfaces/IPluginManager.h"
#include "AppFramework/Public/Widgets/Colors/SComplexGradient.h"
#include "ImageUtils.h"


FTreeItem::FTreeItem(SLightControlTool* InOwningWidget, FString InName, TArray<TSharedPtr<FTreeItem>> InChildren)
    : Name(InName)
    , Children(InChildren)
    , OwningWidget(InOwningWidget)
{
}

FReply FTreeItem::TreeDragDetected(const FGeometry& Geometry, const FPointerEvent& MouseEvent)
{
    GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Green, Name + "Dragggg");


    TSharedRef<FTreeDropOperation> DragDropOp = MakeShared<FTreeDropOperation>();
    DragDropOp->DraggedItem = SharedThis(this);

    FReply Reply = FReply::Handled();

    Reply.BeginDragDrop(DragDropOp);

    return Reply;
}

FReply FTreeItem::TreeDropDetected(const FDragDropEvent& DragDropEvent)
{
    auto DragDrop = StaticCastSharedPtr<FTreeDropOperation>(DragDropEvent.GetOperation());
    GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Green, Name + " Dropped like an anti vax child aged 4");

    auto Target = DragDrop->DraggedItem;
    auto Source = Target->Parent;
    auto Destination = SharedThis(this);

    Source->Children.Remove(Target);
    Destination->Children.Add(Target);
    Target->Parent = Destination;

    OwningWidget->Tree->RequestTreeRefresh();

    auto Reply = FReply::Handled();
    Reply.EndDragDrop();

    return FReply::Handled();
}

void SLightControlTool::Construct(const FArguments& Args)
{
    /*auto ActorSpawnedDelegate = FOnActorSpawned::FDelegate::CreateRaw(this, &SLightControlTool::ActorSpawnedCallback);
    GEngine->GetWorld()->AddOnActorSpawnedHandler(ActorSpawnedDelegate);*/

    LoadResources();
    // Create a test data set
    // TODO: Replace with data from the editor 
    TreeItems = {
        MakeShared<FTreeItem>(this, "Root 1"),
        MakeShared<FTreeItem>(this, "Root 2"),
        MakeShared<FTreeItem>(this, "Root 3"),
        MakeShared<FTreeItem>(this, "Root 4"),
    };

    TreeItems[0]->Children.Add(MakeShared<FTreeItem>(this, "Child 1"));
    TreeItems[0]->Children.Add(MakeShared<FTreeItem>(this, "Child 2"));

    TreeItems[0]->Children[0]->Children.Add(MakeShared<FTreeItem>(this, "Child-child 1"));

    TreeItems[0]->Children[0]->Parent = TreeItems[0];
    TreeItems[0]->Children[1]->Parent = TreeItems[0];

    TreeItems[0]->Children[0]->Children[0]->Parent = TreeItems[0]->Children[0];
       

    // SVerticalBox slots are by default dividing the space equally between each other
    // Because of this we need to expose the slot with the search bar in order to disable that for it
    SVerticalBox::FSlot* LightSearchBarSlot;
    SVerticalBox::FSlot* NewFolderButtonSlot;
    //SHorizontalBox::FSlot* SeparatorSlot;
    FSlateFontInfo Font24(FCoreStyle::GetDefaultFont(), 20);


    GEngine->AddOnScreenDebugMessage(-1, 15.0f, FColor::Emerald, "Light control tool constructed");

    ChildSlot
    [
        SNew(SOverlay)
        + SOverlay::Slot()
        .HAlign(HAlign_Fill)
        .VAlign(VAlign_Top)
        [
            SNew(SSplitter)
            .PhysicalSplitterHandleSize(5.0f)
            .HitDetectionSplitterHandleSize(15.0f)
            +SSplitter::Slot()
            [
                SNew(SVerticalBox) // Light selection menu thingy
                +SVerticalBox::Slot()
                .Expose(LightSearchBarSlot)
                .HAlign(HAlign_Fill)
                .VAlign(VAlign_Top)
                [
                    SNew(SVerticalBox)
                    +SVerticalBox::Slot()
                    .VAlign(VAlign_Top)
                    [
                        SNew(SSearchBox) // Search bar for light
                    ]
                    +SVerticalBox::Slot()
                    .VAlign(VAlign_Top)
                    .AutoHeight()
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("Scene Lights"))
                        .Font(Font24)
                    ]
                ]
                +SVerticalBox::Slot()
                .Padding(0.0f, 0.0f, 8.0f, 0.0f)
                .HAlign(HAlign_Fill)
                .VAlign(VAlign_Top)
                [
                    SNew(SBox)
                    .VAlign(VAlign_Top)
                    [
                        SAssignNew(Tree, STreeView<TSharedPtr<FTreeItem>>)
                        .ItemHeight(24.0f)
                        .TreeItemsSource(&TreeItems)
                        .OnSelectionChanged(this, &SLightControlTool::SelectionCallback)
                        .OnGenerateRow(this, &SLightControlTool::AddToTree)
                        .OnGetChildren(this, &SLightControlTool::GetChildren)
                    ]
                ]
                +SVerticalBox::Slot()
                .VAlign(VAlign_Bottom)
                .Padding(0.0f, 10.0f, 0.0f, 0.0f)
                .Expose(NewFolderButtonSlot)
                [
                    SNew(SButton)
                    .Text(FText::FromString("New Folder"))
                ]
            ]
            + SSplitter::Slot()
            [
                SNew(SHorizontalBox)
                /*+ SHorizontalBox::Slot()
                .Expose(SeparatorSlot)
                .Padding(0.0f, 0.0f, 30.0f, 0.0f)
                [
                    SNew(SSeparator)
                    .Orientation(EOrientation::Orient_Vertical)
                ]         */
                + SHorizontalBox::Slot()
                [
                    SNew(SVerticalBox)                
                    + LightHeader()
                    + LightPropertyEditor()
                ]
            ]
        ]
    ];
    LightSearchBarSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    NewFolderButtonSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    //SeparatorSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    for (auto& TreeItem : TreeItems)
    {
        Tree->SetItemExpansion(TreeItem, true);
    }
}

SLightControlTool::~SLightControlTool()
{
    PreDestroy();
}

void SLightControlTool::PreDestroy()
{
    //if (IntensityGradientTexture)
    {
        IntensityGradientTexture->ConditionalBeginDestroy();
        IntensityGradientTexture->RemoveFromRoot();
        HSVGradientTexture->ConditionalBeginDestroy();
        HSVGradientTexture->RemoveFromRoot();
        SaturationGradientTexture->ConditionalBeginDestroy();
        SaturationGradientTexture->RemoveFromRoot();
        TemperatureGradientTexture->ConditionalBeginDestroy();
        TemperatureGradientTexture->RemoveFromRoot();
        //IntensityGradientTexture.Reset();        
    }

}


TSharedRef<ITableRow> SLightControlTool::AddToTree(TSharedPtr<FTreeItem> Item, const TSharedRef<STableViewBase>& OwnerTable)
{
    return
        SNew(STableRow<TSharedPtr<FString>>, OwnerTable)
        .Padding(2.0f)
        .OnDragDetected(Item.Get(), &FTreeItem::TreeDragDetected)
        .OnDrop_Raw(Item.Get(), &FTreeItem::TreeDropDetected)
        [
            SNew(STextBlock)
            .Text(FText::FromString(Item->Name))
        .ShadowColorAndOpacity(FLinearColor::Blue)
        .ShadowOffset(FIntPoint(-1, 1))
        ];


}

void SLightControlTool::GetChildren(TSharedPtr<FTreeItem> Item, TArray<TSharedPtr<FTreeItem>>& Children)
{
    Children.Append(Item->Children);
}

void SLightControlTool::SelectionCallback(TSharedPtr<FTreeItem> Item, ESelectInfo::Type SelectType)
{
    if (GEngine)
    {
        GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Emerald, Item->Name);
    }
    SelectedItems = Tree->GetSelectedItems();
}

FText SLightControlTool::TestTextGetter() const
{

    FString N = "Nothing Selected";

    if (SelectedItems.Num())
    {
        N = SelectedItems[0]->Name;
    }

    return FText::FromString(N);
}

void SLightControlTool::ActorSpawnedCallback(AActor* Actor)
{
    if (GEngine)
    {
        GEngine->AddOnScreenDebugMessage(-1, 5.0f, FColor::Green, "You just dropped some shit into the level didntya");
    }
}

TArray<FColor> SLightControlTool::LinearGradient(TArray<FColor> ControlPoints, FVector2D Size, EOrientation Orientation)
{
    TArray<FColor> GradientPixels;
    if (ControlPoints.Num())
    {
        if (ControlPoints.Num() == 1)
        {
            for (size_t x = 0; x < Size.X; x++)
            {
                for (size_t y = 0; y < Size.Y; y++)
                {
                    GradientPixels.Add(ControlPoints[0]);
                }
            }
        }
        else
        {
            auto NumSteps = ControlPoints.Num() - 1;
            int TotalStepSize;
            int RepeatCount;
            if (Orientation == Orient_Vertical)
            {
                TotalStepSize = Size.Y;
                RepeatCount = Size.X;
            }
            else
            {
                TotalStepSize = Size.X;
                RepeatCount = Size.Y;
            }
            auto StepSize = TotalStepSize / NumSteps;

            for (auto Rep = 0; Rep < RepeatCount; Rep++)
            {
                for (auto Pixel = 0; Pixel < TotalStepSize; Pixel++)
                {
                    auto Progress = StaticCast<float>(Pixel) / StaticCast<float>(StepSize);
                    auto BeforeId = StaticCast<int>(FMath::Floor(Progress)); // Avoid setting Bef
                    auto Alpha = FMath::Frac(Progress);
                    FLinearColor Before, After;
                    Before = ControlPoints[BeforeId];
                    After = ControlPoints[FMath::Min(ControlPoints.Num() - 1, BeforeId + 1)];

                    auto lc = ((1.0f - Alpha) * Before + Alpha * After);
                    FColor c(lc.ToFColor(false));
                    GradientPixels.Add(c);
                    
                }
            }

        }
    }

    return GradientPixels;
}

TArray<FColor> SLightControlTool::HSVGradient(FVector2D Size, EOrientation Orientation)
{
    int TotalStepSize;
    int RepeatCount;
    if (Orientation == Orient_Vertical)
    {
        TotalStepSize = Size.Y;
        RepeatCount = Size.X;
    }
    else
    {
        TotalStepSize = Size.X;
        RepeatCount = Size.Y;
    }
    TArray<FColor> GradientPixels;

    for (auto Rep = 0; Rep < RepeatCount; Rep++)
    {
        for (auto Pixel = 0; Pixel < TotalStepSize; Pixel++)
        {
            auto H = StaticCast<uint8>(StaticCast<float>(Pixel) / StaticCast<float>(TotalStepSize) * 255);

            GradientPixels.Add(FLinearColor::MakeFromHSV8(H, 255, 255).ToFColor(false));
        }
    }

    return GradientPixels;
}

TSharedPtr<UTexture2D> SLightControlTool::MakeGradientTexture(int X, int Y)
{
    auto Tex = TSharedPtr<UTexture2D>(UTexture2D::CreateTransient(X, Y));
    Tex->CompressionSettings = TextureCompressionSettings::TC_VectorDisplacementmap;
    Tex->SRGB = 0;
    Tex->AddToRoot();
    Tex->UpdateResource();
    return Tex;
}

void SLightControlTool::LoadResources()
{

    
    GenerateTextures();


    


    const FVector2D GradientSize(20.0f, 256.0f);

    IntensityGradientBrush = MakeShared<FSlateImageBrush>(IntensityGradientTexture.Get(), GradientSize);
    HSVGradientBrush = MakeShared<FSlateImageBrush>(HSVGradientTexture.Get(), GradientSize);
    SaturationGradientBrush = MakeShared<FSlateImageBrush>(SaturationGradientTexture.Get(), GradientSize);
    TemperatureGradientBrush = MakeShared<FSlateImageBrush>(TemperatureGradientTexture.Get(), GradientSize);

}

void SLightControlTool::GenerateTextures()
{

    IntensityGradientTexture = MakeGradientTexture();
    HSVGradientTexture = MakeGradientTexture();
    SaturationGradientTexture = MakeGradientTexture();
    TemperatureGradientTexture = MakeGradientTexture();

    auto l = [this](FRHICommandListImmediate& RHICmdList)
    {
        auto UpdateRegion = FUpdateTextureRegion2D(0, 0, 0, 0, 1, 256);

    
        TArray<FColor> IntensityGradientPixels = LinearGradient(TArray<FColor>{
            FColor::Black,
            FColor::White
        });

        TArray<FColor> HSVGradientPixels = LinearGradient(TArray<FColor>{
            FColor::Red,
            FColor::Yellow,
            FColor::Green,
            FColor::Cyan,
            FColor::Blue,
            FColor::Magenta,
            FColor::Red
        });

        TArray<FColor> SaturationGradientPixels = LinearGradient(TArray<FColor>{
            FColor::White,
            FColor::Red
        });

        TArray<FColor> TemperatureGradientPixels = LinearGradient(TArray<FColor>{
            FColor::Cyan,
            FColor::White,
            FColor::Yellow,
            FColor::Red
        });




        RHIUpdateTexture2D(IntensityGradientTexture->GetResource()->GetTexture2DRHI(), 0, UpdateRegion,
            sizeof(FColor), reinterpret_cast<uint8_t*>(IntensityGradientPixels.GetData()));


        RHIUpdateTexture2D(HSVGradientTexture->GetResource()->GetTexture2DRHI(), 0, UpdateRegion,
            sizeof(FColor), reinterpret_cast<uint8_t*>(HSVGradientPixels.GetData()));
        RHIUpdateTexture2D(SaturationGradientTexture->GetResource()->GetTexture2DRHI(), 0, UpdateRegion,
            sizeof(FColor), reinterpret_cast<uint8_t*>(SaturationGradientPixels.GetData()));
        RHIUpdateTexture2D(TemperatureGradientTexture->GetResource()->GetTexture2DRHI(), 0, UpdateRegion,
            sizeof(FColor), reinterpret_cast<uint8_t*>(TemperatureGradientPixels.GetData()));

    };
        //EnqueueUniqueRenderCommand(l);
        ENQUEUE_RENDER_COMMAND(UpdateTextureDataCommand)(l);
        FlushRenderingCommands();
}

SVerticalBox::FSlot& SLightControlTool::LightHeader()
{
    auto& Slot = SVerticalBox::Slot();

    Slot.SizeParam.SizeRule = FSizeParam::SizeRule_Auto;


    Slot
    .HAlign(HAlign_Fill)
        [
            SNew(SHorizontalBox)
            +SHorizontalBox::Slot()
            .HAlign(HAlign_Left)
            [
                SNew(STextBlock)
                .Font(FSlateFontInfo(FCoreStyle::GetDefaultFont(), 18))
                .Text(this, &SLightControlTool::TestTextGetter)
            ]
            +SHorizontalBox::Slot()
            .HAlign(HAlign_Right)
            [
                SNew(SCheckBox)
                .IsEnabled_Lambda([this]() {return SelectedItems.Num() != 0; })
            ]

        ];

    return Slot;
}

SVerticalBox::FSlot& SLightControlTool::LightPropertyEditor()
{
    auto& Slot = SVerticalBox::Slot();

    Slot
    .Padding(20.0f, 30.0f, 20.0f, 0.0f)
    .VAlign(VAlign_Fill)
    .HAlign(HAlign_Fill)
    [
        SNew(SHorizontalBox)
        +SHorizontalBox::Slot() // General light properties + scene parenting thingy from mock-up
        //.MaxWidth(300) // Need to see just how big this is, very much subject to change this one
        [
            SNew(SVerticalBox)
            + GeneralLightPropertyEditor()
            + LightSceneTransformEditor()
        ]
        + LightSpecificPropertyEditor()
    ];


    return Slot;
}

SVerticalBox::FSlot& SLightControlTool::GeneralLightPropertyEditor()
{
    auto& Slot = SVerticalBox::Slot();

    SVerticalBox::FSlot* IntensityNameSlot, * IntensityLumenSlot, * IntensityPercentageSlot;
    SVerticalBox::FSlot* HSVNameSlot, * HSVLumenSlot, * HSVPercentageSlot;
    SVerticalBox::FSlot* SaturationNameSlot, * SaturationLumenSlot, * SaturationPercentageSlot;
    SVerticalBox::FSlot* TemperatureNameSlot, *TemperatureLumenSlot, *TemperaturePercentageSlot;

    

    Slot
    [
        SNew(SHorizontalBox)
        +SHorizontalBox::Slot() // Intensity slider
        .VAlign(VAlign_Fill)
        [
            SNew(SBorder)
            .VAlign(VAlign_Fill)
            .HAlign(HAlign_Fill)
            .ColorAndOpacity(FLinearColor::Gray)
            [
                SNew(SVerticalBox)
                +SVerticalBox::Slot()
                .Expose(IntensityNameSlot)
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("Intensity"))
                ]
                +SVerticalBox::Slot()
                .Expose(IntensityLumenSlot)
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("300 lumens"))
                ]
                +SVerticalBox::Slot()
                [
                    SNew(SHorizontalBox)
                    + SHorizontalBox::Slot()
                    .HAlign(HAlign_Right)
                    [
                        SNew(SImage)
                        .Image(IntensityGradientBrush.Get())
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Left)
                    [
                        SNew(SSlider)
                        .Orientation(EOrientation::Orient_Vertical)
                    ]
                ]
                +SVerticalBox::Slot()
                .Expose(IntensityPercentageSlot)
                [
                    SNew(STextBlock)
                    .Justification(ETextJustify::Center)
                    .Text(FText::FromString("50%"))
                ]
            ]
        ]
        + SHorizontalBox::Slot()
            .VAlign(VAlign_Fill)
        [
            SNew(SBorder)
            .VAlign(VAlign_Fill)
            .HAlign(HAlign_Fill)
            .ColorAndOpacity(FLinearColor::Gray)
            [
                SNew(SVerticalBox)
                +SVerticalBox::Slot()
                .Expose(HSVNameSlot)
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("Hue"))
                ]
                +SVerticalBox::Slot()
                .Expose(HSVLumenSlot)
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("360"))
                ]
                +SVerticalBox::Slot()
                [
                    SNew(SHorizontalBox)
                    + SHorizontalBox::Slot()
                    .HAlign(HAlign_Right)
                    [
                        SNew(SImage)
                        .Image(HSVGradientBrush.Get())
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Left)
                    [
                        SNew(SSlider)
                        .Orientation(EOrientation::Orient_Vertical)
                    ]
                ]
                +SVerticalBox::Slot()
                .Expose(HSVPercentageSlot)
                [
                    SNew(STextBlock)
                    .Justification(ETextJustify::Center)
                    .Text(FText::FromString("50%"))
                ]
            ]
        ]
        + SHorizontalBox::Slot()
            .VAlign(VAlign_Fill)
        [
            SNew(SBorder)
            .VAlign(VAlign_Fill)
            .HAlign(HAlign_Fill)
            .ColorAndOpacity(FLinearColor::Gray)
            [
                SNew(SVerticalBox)
                +SVerticalBox::Slot()
                .Expose(SaturationNameSlot)
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("Saturation"))
                ]
                +SVerticalBox::Slot()
                .Expose(SaturationLumenSlot)
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("33%"))
                ]
                +SVerticalBox::Slot()
                [
                    SNew(SHorizontalBox)
                    + SHorizontalBox::Slot()
                    .HAlign(HAlign_Right)
                    [
                        SNew(SImage)
                        .Image(SaturationGradientBrush.Get())
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Left)
                    [
                        SNew(SSlider)
                        .Orientation(EOrientation::Orient_Vertical)
                    ]
                ]
                +SVerticalBox::Slot()
                .Expose(SaturationPercentageSlot)
                [
                    SNew(STextBlock)
                    .Justification(ETextJustify::Center)
                    .Text(FText::FromString("50%"))
                ]
            ]
        ]
        + SHorizontalBox::Slot()
            .VAlign(VAlign_Fill)
        [
            SNew(SBorder)
            .VAlign(VAlign_Fill)
            .HAlign(HAlign_Fill)
            .ColorAndOpacity(FLinearColor::Gray)
            [
                SNew(SVerticalBox)
                +SVerticalBox::Slot()
                .Expose(TemperatureNameSlot)
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("Temperature"))
                ]
                +SVerticalBox::Slot()
                .Expose(TemperatureLumenSlot)
                [
                    SNew(STextBlock)
                    .Text(FText::FromString("3000 Kelvin"))
                ]
                +SVerticalBox::Slot()
                [
                    SNew(SHorizontalBox)
                    + SHorizontalBox::Slot()
                    .HAlign(HAlign_Right)
                    [
                        SNew(SImage)
                        .Image(TemperatureGradientBrush.Get())
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Left)
                    [
                        SNew(SSlider)
                        .Orientation(EOrientation::Orient_Vertical)
                    ]
                ]
                +SVerticalBox::Slot()
                .Expose(TemperaturePercentageSlot)
                [
                    SNew(STextBlock)
                    .Justification(ETextJustify::Center)
                    .Text(FText::FromString("71%"))
                ]
            ]
        ]

    ];

    IntensityNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    IntensityLumenSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    IntensityPercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    HSVNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    HSVLumenSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    HSVPercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    SaturationNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    SaturationLumenSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    SaturationPercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    TemperatureNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    TemperatureLumenSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    TemperaturePercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;


    return Slot;
}

SVerticalBox::FSlot& SLightControlTool::LightSceneTransformEditor()
{
    auto& Slot = SVerticalBox::Slot();
    Slot.SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    SHorizontalBox::FSlot* ButtonsSlot;

    Slot
    .Padding(0.0f, 5.0f, 0.0f, 0.0f)
    [
        SNew(SBorder)
        .HAlign(HAlign_Fill)
        .VAlign(VAlign_Fill)
        [
            SNew(SHorizontalBox)
            +SHorizontalBox::Slot()
            .HAlign(HAlign_Fill)
            [
                SNew(SVerticalBox)
                +SVerticalBox::Slot()
                .VAlign(VAlign_Center)
                .HAlign(HAlign_Fill)
                .Padding(5.0f, 3.0f)
                [
                    SNew(SHorizontalBox)
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Fill)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("Parent Object"))
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Fill)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("none"))
                    ]
                ]
                +SVerticalBox::Slot()
                .VAlign(VAlign_Center)
                .HAlign(HAlign_Fill)
                .Padding(5.0f, 3.0f)
                [
                    SNew(SHorizontalBox)
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Fill)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("Position"))
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Fill)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("0.0; 0.0; 0.0"))
                    ]
                ]
                +SVerticalBox::Slot()
                .VAlign(VAlign_Center)
                .HAlign(HAlign_Fill)
                .Padding(5.0f, 3.0f)
                [
                    SNew(SHorizontalBox)
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Fill)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("Rotation"))
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Fill)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("0.0; 0.0; 270.0"))
                    ]
                ]
                +SVerticalBox::Slot()
                .VAlign(VAlign_Center)
                .HAlign(HAlign_Fill)
                .Padding(5.0f, 3.0f)
                [
                    SNew(SHorizontalBox)
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Fill)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("Scale"))
                    ]
                    +SHorizontalBox::Slot()
                    .HAlign(HAlign_Fill)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("1.0; 1.0; 1.0"))
                    ]
                ]
            ]
            +SHorizontalBox::Slot()
            .Expose(ButtonsSlot)
            [
                SNew(SVerticalBox)
                +SVerticalBox::Slot()
                .Padding(5.0f)
                [
                    SNew(SButton)
                    .Text(FText::FromString("Select Scene Object"))
                ]
                + SVerticalBox::Slot()
                .Padding(5.0f)
                [
                    SNew(SButton)
                    .Text(FText::FromString("Select Parent Object"))
                ]
            ]
        ]
    ];

    ButtonsSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    return Slot;
}

SHorizontalBox::FSlot& SLightControlTool::LightSpecificPropertyEditor()
{
    auto& Slot = SHorizontalBox::Slot();
    Slot.SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    SVerticalBox::FSlot* HorizontalNameSlot, * HorizontalDegreesSlot, * HorizontalPercentageSlot;
    SVerticalBox::FSlot* VerticalNameSlot, * VerticalDegreesSlot, * VerticalPercentageSlot;
    SVerticalBox::FSlot* AngleNameSlot, *AngleDegreesSlot, *AnglePercentageSlot;

    Slot
    .Padding(5.0f, 0.0f, 0.0f, 0.0f)
    [
        SNew(SBorder)
        .HAlign(HAlign_Fill)
        .VAlign(VAlign_Fill)
        [
            SNew(SVerticalBox)
            +SVerticalBox::Slot()
            [
                SNew(SHorizontalBox)
                +SHorizontalBox::Slot()
                .Padding(5.0f)
                [
                    SNew(SVerticalBox)
                    +SVerticalBox::Slot()
                    .Expose(HorizontalNameSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("Horizontal"))
                    ]
                    +SVerticalBox::Slot()
                    .Expose(HorizontalDegreesSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("0"))
                    ]
                    +SVerticalBox::Slot()
                    [
                        SNew(SSlider)
                        .Orientation(Orient_Vertical)
                    ]
                    + SVerticalBox::Slot()
                    .Expose(HorizontalPercentageSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("50%"))
                    ]
                ]
                +SHorizontalBox::Slot()
                .Padding(5.0f)
                [
                    SNew(SVerticalBox)
                    +SVerticalBox::Slot()
                    .Expose(VerticalNameSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("Vertical"))
                    ]
                    +SVerticalBox::Slot()
                    .Expose(VerticalDegreesSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("0"))
                    ]
                    +SVerticalBox::Slot()
                    [
                        SNew(SSlider)
                        .Orientation(Orient_Vertical)
                    ]
                    + SVerticalBox::Slot()
                    .Expose(VerticalPercentageSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("50%"))
                    ]
                ]
                +SHorizontalBox::Slot()
                .Padding(5.0f)
                [
                    SNew(SVerticalBox)
                    +SVerticalBox::Slot()
                    .Expose(AngleNameSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("Angle"))
                    ]
                    +SVerticalBox::Slot()
                    .Expose(AngleDegreesSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("0"))
                    ]
                    +SVerticalBox::Slot()
                    [
                        SNew(SSlider)
                        .Orientation(Orient_Vertical)
                    ]
                    + SVerticalBox::Slot()
                    .Expose(AnglePercentageSlot)
                    [
                        SNew(STextBlock)
                        .Text(FText::FromString("50%"))
                    ]
                ]
            ]
        ]
    ];

    HorizontalNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    HorizontalDegreesSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    HorizontalPercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    VerticalNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    VerticalDegreesSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    VerticalPercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;

    AngleNameSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    AngleDegreesSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;
    AnglePercentageSlot->SizeParam.SizeRule = FSizeParam::SizeRule_Auto;



    return Slot;
}
