#include "PhysicalObjectTrackerEditor.h"

#include "ContentBrowserModule.h"
#include "PhysicalObjectTrackingReferenceCalibrationHandler.h"

#define LOCTEXT_NAMESPACE "FPhysicalObjectTrackerEditor"

void FPhysicalObjectTrackerEditor::StartupModule()
{
	m_TrackingCalibrationHandler = MakeUnique<FPhysicalObjectTrackingReferenceCalibrationHandler>();

	FContentBrowserModule& ContentBrowserModule = FModuleManager::LoadModuleChecked<FContentBrowserModule>(TEXT("ContentBrowser"));
	TArray<FContentBrowserMenuExtender_SelectedAssets>& CBMenuAssetExtenderDelegates = ContentBrowserModule.GetAllAssetViewContextMenuExtenders();
	CBMenuAssetExtenderDelegates.Add(FContentBrowserMenuExtender_SelectedAssets::CreateRaw(m_TrackingCalibrationHandler.Get(), &FPhysicalObjectTrackingReferenceCalibrationHandler::CreateMenuExtender));

}

void FPhysicalObjectTrackerEditor::ShutdownModule()
{
	m_TrackingCalibrationHandler = nullptr;
}


#undef LOCTEXT_NAMESPACE

IMPLEMENT_MODULE(FPhysicalObjectTrackerEditor, PhysicalObjectTrackerEditor)