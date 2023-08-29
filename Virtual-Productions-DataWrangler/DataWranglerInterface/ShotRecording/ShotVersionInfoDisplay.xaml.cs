﻿using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using AutoNotify;
using CommonLogging;
using DataApiCommon;
using DataWranglerCommon;
using DataWranglerCommon.IngestDataSources;
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
		[AutoNotify] 
		private DataEntityShotVersion? m_currentVersion = null;
		
		private IngestDataShotVersionMeta m_currentVersionMeta = new IngestDataShotVersionMeta();

		private bool m_isInBatchMetaChange = false;

		public ShotVersionInfoDisplay()
		{
			InitializeComponent();

			AsyncOperationChangeFeedback feedbackElement = new AsyncOperationChangeFeedback();
			DependencyObject fileSourceParent = VersionFileSourcesControl.Parent;
			ContentPropertyAttribute? contentAttribute = fileSourceParent.GetType().GetCustomAttribute<ContentPropertyAttribute>(true);
			if (contentAttribute != null)
			{
				PropertyInfo? prop = fileSourceParent.GetType().GetProperty(contentAttribute.Name, BindingFlags.Instance | BindingFlags.Public);
				if (prop != null)
				{
					UIElementCollection? collection = (UIElementCollection?)prop.GetValue(fileSourceParent);
					if (collection != null)
					{
						collection.Remove(VersionFileSourcesControl);
						collection.Add(feedbackElement);
						feedbackElement.Children.Add(VersionFileSourcesControl);
					}
					else
					{
						throw new Exception($"Failed to find UI Collection on {fileSourceParent}");
					}
				}
				else
				{
					throw new Exception($"Could not find property with name {contentAttribute.Name} on type {fileSourceParent.GetType()}");
				}
			}
			else
			{
				throw new Exception($"{fileSourceParent.GetType()} does not specify a ContentPropertyAttribute");
			}


			VersionSelectorControl.OnShotVersionSelected += OnShotVersionSelected;
		}

		public void SetParentControls(ShotRecordingPage a_parentPage)
		{
			a_parentPage.OnNewShotVersionCreationStarted += (_) => VersionSelectorControl.BeginAddShotVersion();
			a_parentPage.OnNewShotVersionCreated += (a_data) => VersionSelectorControl.EndAddShotVersion(a_data);
		}

		public void OnShotSelected(Guid a_shotId)
		{
			VersionSelectorControl.AsyncRefreshShotVersion(a_shotId);
		}

		private void OnShotVersionSelected(DataEntityShotVersion? a_shotVersion)
		{
			if (a_shotVersion != null)
			{
				if (CurrentVersion != null)
				{
					CurrentVersion.PropertyChanged -= OnCurrentVersionPropertyChanged;
				}

				CurrentVersion = a_shotVersion;
				CurrentVersion.PropertyChanged += OnCurrentVersionPropertyChanged;

				if (!string.IsNullOrEmpty(a_shotVersion.DataWranglerMeta))
				{
					try
					{
						IngestDataShotVersionMeta? shotMeta = JsonConvert.DeserializeObject<IngestDataShotVersionMeta>(a_shotVersion.DataWranglerMeta, DataWranglerSerializationSettings.Instance);
						if (shotMeta != null)
						{
							SetTargetMeta(shotMeta);
						}
						else
						{
							Logger.LogError("Interface",
								$"Failed to deserialize shot version meta from value: {a_shotVersion.DataWranglerMeta}");
							SetTargetMeta(new IngestDataShotVersionMeta());
						}
					}
					catch (JsonSerializationException ex)
					{
						Logger.LogError("Interface", $"Failed to deserialize shot version meta from value: {a_shotVersion.DataWranglerMeta}. Exception: {ex.Message}");
						SetTargetMeta(new IngestDataShotVersionMeta());
					}
					catch (JsonReaderException ex)
					{
						Logger.LogError("Interface", $"Failed to deserialize shot version meta from value: {a_shotVersion.DataWranglerMeta}. Exception: {ex.Message}");
						SetTargetMeta(new IngestDataShotVersionMeta());
					}
				}
				else
				{
					SetTargetMeta(new IngestDataShotVersionMeta());
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
			if (m_currentVersion == null)
			{
				Logger.LogWarning("Interface", "Tried to update ShotGridMeta for invalid shot version.");
				return;
			}

			m_currentVersion.DataWranglerMeta = JsonConvert.SerializeObject(m_currentVersionMeta, DataWranglerSerializationSettings.Instance);
			Task<DataApiResponseGeneric> updateTask = m_currentVersion.ChangeTracker.CommitChanges(DataWranglerServiceProvider.Instance.TargetDataApi);
			updateTask.ContinueWith((a_task) => {
				if (a_task.Result.IsError)
				{
					throw new Exception();
				}
				else
				{
					DataEntityShotVersion updatedEntity = (DataEntityShotVersion)a_task.Result.ResultDataGeneric;

					VersionSelectorControl.UpdateEntity(updatedEntity);
					if (!string.IsNullOrEmpty(updatedEntity.DataWranglerMeta))
					{
						IngestDataShotVersionMeta? updatedMeta = JsonConvert.DeserializeObject<IngestDataShotVersionMeta>(updatedEntity.DataWranglerMeta, DataWranglerSerializationSettings.Instance);
						if (updatedMeta != null)
						{
							SetTargetMeta(updatedMeta);
						}
					}
				}
			});
			AsyncOperationChangeFeedback? feedbackElement = AsyncOperationChangeFeedback.FindFeedbackElementFrom(VersionFileSourcesControl);
			if (feedbackElement != null)
			{
				feedbackElement.ProvideFeedback(updateTask);
			}
		}

		private void SetTargetMeta(IngestDataShotVersionMeta a_meta)
		{
			foreach (IngestDataSourceMeta meta in m_currentVersionMeta.FileSources)
			{
				meta.PropertyChanged -= OnAnyMetaPropertyChanged;
			}

			m_currentVersionMeta = a_meta;

			foreach (IngestDataSourceMeta meta in m_currentVersionMeta.FileSources)
			{
				meta.PropertyChanged += OnAnyMetaPropertyChanged;
			}

			VersionFileSourcesControl.SetCurrentMeta(m_currentVersionMeta);
		}

		private void OnCurrentVersionPropertyChanged(object? a_sender, PropertyChangedEventArgs a_e)
		{
			if (CurrentVersion == null)
			{
				return;
			}

			
			if (a_e.PropertyName == nameof(CurrentVersion.Description))
			{
				Task<DataApiResponseGeneric> task = CurrentVersion.ChangeTracker.CommitChanges(DataWranglerServiceProvider.Instance.TargetDataApi);
				DescriptionFeedbackElement.ProvideFeedback(task);
			}
			else if (a_e.PropertyName == nameof(CurrentVersion.Flagged))
			{
				Task<DataApiResponseGeneric> task = CurrentVersion.ChangeTracker.CommitChanges(DataWranglerServiceProvider.Instance.TargetDataApi);
				GoodTakeFeedbackElement.ProvideFeedback(task);
			}
		}
	}
}
