using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using AutoNotify;
using CommonLogging;
using DataWranglerCommon;
using DataWranglerCommonWPF;
using Microsoft.Win32;
using Newtonsoft.Json;
using ShotGridIntegration;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for ShotVersionInfoDisplay.xaml
	/// </summary>
	public partial class ShotVersionInfoDisplay : UserControl
	{
		[AutoNotify] private ShotGridEntityShotVersion? m_currentVersion = null;
		private DataWranglerShotVersionMeta m_currentVersionMeta = new DataWranglerShotVersionMeta();

		private bool m_isInBatchMetaChange = false;

		public ShotVersionInfoDisplay()
		{
			InitializeComponent();
			
			VersionSelectorControl.OnShotVersionSelected += OnShotVersionSelected;
		}

		public void SetParentControls(ShotRecordingPage a_parentPage)
		{
			a_parentPage.OnNewShotVersionCreationStarted += (_) => VersionSelectorControl.BeginAddShotVersion();
			a_parentPage.OnNewShotVersionCreated += (a_data) => VersionSelectorControl.EndAddShotVersion(a_data);
		}

		public void OnShotSelected(int a_shotId)
		{
			VersionSelectorControl.AsyncRefreshShotVersion(a_shotId);
		}

		private void OnShotVersionSelected(ShotGridEntityShotVersion? a_shotVersion)
		{
			if (a_shotVersion != null)
			{
				CurrentVersion = a_shotVersion;

				if (!string.IsNullOrEmpty(a_shotVersion.Attributes.DataWranglerMeta))
				{
					try
					{
						DataWranglerShotVersionMeta? shotMeta = JsonConvert.DeserializeObject<DataWranglerShotVersionMeta>(a_shotVersion.Attributes.DataWranglerMeta, DataWranglerSerializationSettings.Instance);
						if (shotMeta != null)
						{
							SetTargetMeta(shotMeta);
						}
						else
						{
							Logger.LogError("Interface",
								$"Failed to deserialize shot version meta from value: {a_shotVersion.Attributes.DataWranglerMeta}");
							SetTargetMeta(new DataWranglerShotVersionMeta());
						}
					}
					catch (JsonSerializationException ex)
					{
						Logger.LogError("Interface", $"Failed to deserialize shot version meta from value: {a_shotVersion.Attributes.DataWranglerMeta}. Exception: {ex.Message}");
						SetTargetMeta(new DataWranglerShotVersionMeta());
					}
					catch (JsonReaderException ex)
					{
						Logger.LogError("Interface", $"Failed to deserialize shot version meta from value: {a_shotVersion.Attributes.DataWranglerMeta}. Exception: {ex.Message}");
						SetTargetMeta(new DataWranglerShotVersionMeta());
					}
				}
				else
				{
					SetTargetMeta(new DataWranglerShotVersionMeta());
				}
			}
		}

		private void OnAnyMetaPropertyChanged(object? a_sender, PropertyChangedEventArgs a_e)
		{
			if (!m_isInBatchMetaChange)
			{
				UpdateRemoteShotGridMetaField();
			}
		}

		private void UpdateRemoteShotGridMetaField()
		{
			int selectedVersionId = VersionSelectorControl.SelectedVersionEntityId;
			if (selectedVersionId == -1)
			{
				Logger.LogWarning("Interface", "Tried to update ShotGridMeta for invalid shot version.");
				return;
			}

			string metaAsString = JsonConvert.SerializeObject(m_currentVersionMeta, DataWranglerSerializationSettings.Instance);
			Dictionary<string, object> valuesToSet = new Dictionary<string, object> { { "sg_datawrangler_meta", metaAsString } };

			Task<ShotGridAPIResponse<ShotGridEntityShotVersion>> response = DataWranglerServiceProvider.Instance.ShotGridAPI.UpdateEntityProperties<ShotGridEntityShotVersion>(
				selectedVersionId, valuesToSet);
			response.ContinueWith((a_task) => {
				if (a_task.Result.IsError)
				{
					throw new Exception();
				}
				else
				{
					VersionSelectorControl.UpdateEntity(response.Result.ResultData!);
					if (!string.IsNullOrEmpty(a_task.Result.ResultData.Attributes.DataWranglerMeta))
					{
						DataWranglerShotVersionMeta? updatedMeta = JsonConvert.DeserializeObject<DataWranglerShotVersionMeta>(a_task.Result.ResultData.Attributes.DataWranglerMeta, DataWranglerSerializationSettings.Instance);
						if (updatedMeta != null)
						{
							SetTargetMeta(updatedMeta);
						}
					}
				}
			});
		}

		private void SetTargetMeta(DataWranglerShotVersionMeta a_meta)
		{
			foreach (DataWranglerFileSourceMeta meta in m_currentVersionMeta.FileSources)
			{
				meta.PropertyChanged -= OnAnyMetaPropertyChanged;
			}

			m_currentVersionMeta = a_meta;

			foreach (DataWranglerFileSourceMeta meta in m_currentVersionMeta.FileSources)
			{
				meta.PropertyChanged += OnAnyMetaPropertyChanged;
			}

			VersionFileSourcesControl.SetCurrentMeta(m_currentVersionMeta);
		}

		private void GoodTakeCheckbox_Clicked(object a_sender, RoutedEventArgs a_e)
		{
			if (CurrentVersion == null || !CurrentVersion.ChangeTracker.HasAnyUncommittedChanges())
			{
				return;
			}

			Task<ShotGridAPIResponseGeneric> task = CurrentVersion.ChangeTracker.CommitChanges(DataWranglerServiceProvider.Instance.ShotGridAPI);
			FrameworkElement sender = (FrameworkElement) a_sender;
			AsyncOperationChangeFeedback? feedbackElement = AsyncOperationChangeFeedback.FindFeedbackElementFrom(sender);
			if (feedbackElement != null)
			{
				feedbackElement.ProvideFeedback(task);
			}
		}

		private void OnDescriptionChanged(object a_sender, RoutedEventArgs a_routedEventArgs)
		{
			if (CurrentVersion == null || !CurrentVersion.ChangeTracker.HasAnyUncommittedChanges())
			{
				return;
			}

			Task<ShotGridAPIResponseGeneric> task = CurrentVersion.ChangeTracker.CommitChanges(DataWranglerServiceProvider.Instance.ShotGridAPI);
			FrameworkElement sender = (FrameworkElement)a_sender;
			AsyncOperationChangeFeedback? feedbackElement = AsyncOperationChangeFeedback.FindFeedbackElementFrom(sender);
			if (feedbackElement != null)
			{
				feedbackElement.ProvideFeedback(task);
			}
		}
	}
}
