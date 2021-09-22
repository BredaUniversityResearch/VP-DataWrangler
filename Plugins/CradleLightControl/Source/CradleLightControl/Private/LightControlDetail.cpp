#include "LightControlDetail.h"

#include "Slate.h"

#include "DetailCategoryBuilder.h"
#include "DetailLayoutBuilder.h"
#include "DetailWidgetRow.h"
#include "Engine/Engine.h"

void FLightControlDetailCustomization::CustomizeDetails(IDetailLayoutBuilder& DetailBuilder)
{
    if (GEngine)
    {
        GEngine->AddOnScreenDebugMessage(-1, 300.0f, FColor::Orange, "IT'S ALIVE");
    }
    FName N1, N2;
    N1 = ("The Blackjack");
    N2 = ("And the hookers");
    auto& Category = DetailBuilder.EditCategory("Blackjack and hookers");
    Category.AddCustomRow(FText::FromName(N1))
        [
            SNew(SBorder)
            [
                SNew(STextBlock)
                .Text(FText::FromName(N1))
            ]
        ];
   
    //[
    //    /*SNew(SComboBox<TSharedRef<FString>>)
    //    [
    //        SNew(STextBlock)
    //    ]*/
    //    /*
    //    [
    //        SNew(SButton)
    //    ]
    //    [
    //        SNew(SButton)
    //    ]
    //    [
    //        SNew(SButton)
    //    ]*/
    //];

}

TSharedRef<IDetailCustomization> FLightControlDetailCustomization::MakeInstance()
{
    return MakeShareable(new FLightControlDetailCustomization);
}
