using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using DataWranglerCommon;
using DataWranglerInterface.DebugSupport;
using Newtonsoft.Json;
using ShotGridIntegration;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for ShotVersionDisplayControl.xaml
	/// </summary>
	public partial class ShotVersionDisplayControl : UserControl
	{
		private int m_selectedShotId = -1;

		private ProjectSelectorControl? m_projectSelector = null;
		private DataWranglerShotVersionMeta m_currentVersionMeta = new DataWranglerShotVersionMeta();

		private bool m_isInBatchMetaChange = false;
		private bool m_shouldCreateNewShotOnRecord = true;

		public ShotVersionDisplayControl()
		{
			InitializeComponent();

			VideoMetaControl.UpdateData(m_currentVersionMeta);

			CreateNewTake.Click += OnCreateNewShotVersion;

			AudioSource.Items.Add("Embedded");
			AudioSource.Items.Add("External Audio Recorder");
			AudioSource.SelectionChanged += OnAudioSourceChanged;

			VersionSelectorControl.OnShotVersionSelected += OnShotVersionSelected;
			AutoCreateNewTake.Checked += (_, _) =>
			{
				m_shouldCreateNewShotOnRecord = AutoCreateNewTake.IsChecked ?? false;
			};
		}

		public void SetProjectSelector(ProjectSelectorControl a_projectSelector)
		{
			m_projectSelector = a_projectSelector;
		}

		private void OnCreateNewShotVersion(object a_sender, RoutedEventArgs a_e)
		{
			CreateNewShot(new DataWranglerShotVersionMeta());
		}

		private void CreateNewShot(DataWranglerShotVersionMeta a_meta)
		{
			if (m_projectSelector == null)
			{
				throw new Exception();
			}

			if (m_selectedShotId != -1)
			{
				int nextTakeId = VersionSelectorControl.GetHighestVersionNumber() + 1;

				VersionSelectorControl.BeginAddShotVersion();

				ShotGridEntityShotVersion.ShotVersionAttributes attributes =
					new ShotGridEntityShotVersion.ShotVersionAttributes();
				attributes.VersionCode = $"Take {nextTakeId:D2}";
				attributes.DataWranglerMeta =
					JsonConvert.SerializeObject(a_meta, DataWranglerSerializationSettings.Instance);

				DataWranglerServiceProvider.Instance.ShotGridAPI.CreateNewShotVersion(
					m_projectSelector.SelectedProjectId, m_selectedShotId,
					attributes).ContinueWith(a_result =>
				{
					if (!a_result.Result.IsError)
					{
						VersionSelectorControl.EndAddShotVersion(a_result.Result.ResultData);
					}
				});
			}
		}

		public void OnShotSelected(int a_shotId)
		{
			m_selectedShotId = a_shotId;
			VersionSelectorControl.AsyncRefreshShotVersion(a_shotId);
		}

		private void OnShotVersionSelected(ShotGridEntityShotVersion? a_shotVersion)
		{
			if (a_shotVersion != null)
			{
				if (!string.IsNullOrEmpty(a_shotVersion.Attributes.DataWranglerMeta))
				{
					try
					{
						DataWranglerShotVersionMeta? shotMeta = JsonConvert.DeserializeObject<DataWranglerShotVersionMeta>(a_shotVersion.Attributes.DataWranglerMeta);
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
				EShotGridEntity.Version, selectedVersionId, valuesToSet);
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
						DataWranglerShotVersionMeta? updatedMeta = JsonConvert.DeserializeObject<DataWranglerShotVersionMeta>(a_task.Result.ResultData.Attributes.DataWranglerMeta);
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
			m_currentVersionMeta.Video.PropertyChanged -= OnAnyMetaPropertyChanged;

			m_currentVersionMeta = a_meta;

			m_currentVersionMeta.Video.PropertyChanged += OnAnyMetaPropertyChanged;

			VideoMetaControl.UpdateData(m_currentVersionMeta);

			SetValuesFromMeta(m_currentVersionMeta);
		}

		private void SetValuesFromMeta(DataWranglerShotVersionMeta a_meta)
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.InvokeAsync(() => SetValuesFromMeta(a_meta));
				return;
			}

			AudioSource.SelectedItem = a_meta.Audio.Source;
		}

		private void OnAudioSourceChanged(object a_sender, SelectionChangedEventArgs a_e)
		{
			m_currentVersionMeta.Audio.Source = (string)(AudioSource.SelectedItem);
		}

		public void OnActiveCameraRecordingStateChanged(ActiveCameraInfo a_camera, bool a_isNowRecording, DateTimeOffset a_stateChangeTime)
		{
			if (a_isNowRecording)
			{
				DataWranglerShotVersionMeta targetMeta;
				if (m_shouldCreateNewShotOnRecord)
				{
					targetMeta = m_currentVersionMeta.Clone();
				}
				else
				{
					targetMeta = m_currentVersionMeta;
				}

				m_isInBatchMetaChange = true;

				targetMeta.Video.RecordingStart = a_stateChangeTime;
				targetMeta.Video.StorageTarget = a_camera.CurrentStorageTarget;
				targetMeta.Video.CodecName = a_camera.SelectedCodec;

				m_isInBatchMetaChange = false;
				if (m_shouldCreateNewShotOnRecord)
				{
					CreateNewShot(targetMeta);
				}
				else
				{
					UpdateRemoteShotGridMetaField();
				}
			}
		}
	}
}
