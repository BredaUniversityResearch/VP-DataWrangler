using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using AutoNotify;
using BlackmagicCameraControlData;
using CommonLogging;
using DataApiCommon;
using DataWranglerCommon;
using DataWranglerCommon.CameraHandling;
using DataWranglerCommon.IngestDataSources;
using Newtonsoft.Json;
using ShotGridIntegration;

namespace DataWranglerInterface.ShotRecording
{
    /// <summary>
    /// Interaction logic for ShotVersionTemplateDisplay.xaml
    /// </summary>
    public partial class ShotVersionTemplateDisplay : UserControl
	{
		private class CameraPropertyChangedSubscriber
		{
			public IngestDataShotVersionMeta Meta;
			public ActiveCameraInfo CameraTarget;

			public CameraPropertyChangedSubscriber(IngestDataShotVersionMeta a_meta, ActiveCameraInfo a_cameraInfo)
			{
				Meta = a_meta;
				CameraTarget = a_cameraInfo;
			}

			public void OnCameraPropertyChanged(object? a_sender, CameraPropertyChangedEventArgs a_e)
			{
				if (a_e.PropertyName != nameof(CameraTarget.SelectedCodec))
				{
					return;
				}

				foreach (IngestDataSourceMeta source in Meta.FileSources)
				{
					if (source is IngestDataSourceMetaBlackmagicUrsa ursaSource)
					{ 
						ursaSource.CodecName = CameraTarget.SelectedCodec;
					}
				}
			}
		};

		private ShotRecordingPage? m_parentPage = null;

		private DataEntityShot? m_targetShot = null;
		private ProjectSelectorControl? m_projectSelector = null;
		private ShotSelectorControl? m_shotSelectorControl = null;

		private bool m_shouldCreateNewShotOnRecord = true;

		[AutoNotify]
		private IngestDataShotVersionMeta? m_targetShotIngestData = null;

		//Subscriber for hooking into StorageTargetChanged / CodecChanged messages during recording.
		private CameraPropertyChangedSubscriber? m_subscriber = null;

		public ShotVersionTemplateDisplay()
		{
			InitializeComponent();
			
			AutoCreateNewTake.Click += (_, _) =>
			{
				m_shouldCreateNewShotOnRecord = AutoCreateNewTake.IsChecked ?? false;
			};
		}

		public void SetParentControls(ShotRecordingPage a_parentPage, ProjectSelectorControl a_projectSelector, ShotSelectorControl a_shotSelector)
		{
			m_parentPage = a_parentPage;
			m_projectSelector = a_projectSelector;
			m_shotSelectorControl = a_shotSelector;
		}

		private void CreateNewShotVersion(IngestDataShotVersionMeta a_meta)
		{
			if (m_shotSelectorControl == null || m_projectSelector == null)
			{
				throw new Exception();
			}

			Guid targetShotId = m_shotSelectorControl.SelectedShotId;
			if (targetShotId != Guid.Empty)
			{
				m_parentPage?.BeginAddShotVersion(targetShotId);
				DataWranglerServiceProvider.Instance.TargetDataApi.GetVersionsForShot(targetShotId, (a_lhs, a_rhs) => string.Compare(a_lhs.ShotVersionName, a_rhs.ShotVersionName, StringComparison.Ordinal)).ContinueWith(a_task => {
					if (!a_task.Result.IsError)
					{
						int nextTakeId = FindNextTakeIdFromShotVersions(a_task.Result.ResultData) + 1;

						ConfigStringBuilder sb = new ConfigStringBuilder();
						sb.AddReplacement("ShotVersionId", nextTakeId.ToString("D2"));

						DataEntityShotVersion newVersion = new DataEntityShotVersion
						{
							ShotVersionName = Configuration.DataWranglerConfig.Instance.ShotVersionNameTemplate.Build(sb),
							DataWranglerMeta = JsonConvert.SerializeObject(a_meta, DataWranglerSerializationSettings.Instance)
						};

						DataWranglerServiceProvider.Instance.TargetDataApi.CreateNewShotVersion(
							m_projectSelector.SelectedProjectId, targetShotId, newVersion)
							.ContinueWith(a_result =>
						{
							if (!a_result.Result.IsError)
							{
								m_parentPage?.CompleteAddShotVersion(a_result.Result.ResultData);
                                DataWranglerEventDelegates.Instance.NotifyShotCreated(a_result.Result
                                    .ResultData.EntityId);
                            }
						});
					}
				});
			}
		}

		private int FindNextTakeIdFromShotVersions(DataEntityShotVersion[] a_resultData)
		{
			ConfigStringBuilder sb = new ConfigStringBuilder();
			sb.AddReplacement("ShotVersionId", "([0-9]{2})");
			Regex shotNameTemplateMatcher = new Regex(Configuration.DataWranglerConfig.Instance.ShotVersionNameTemplate.Build(sb));

			int nextShotId = 0;
			foreach (DataEntityShotVersion shotVersion in a_resultData)
			{
				Match nameMatch = shotNameTemplateMatcher.Match(shotVersion.ShotVersionName);
				if (nameMatch.Success && nameMatch.Groups.Count >= 1)
				{
					nextShotId = int.Parse(nameMatch.Groups[1].ValueSpan);
					break;
				}
			}

			return nextShotId;
		}

		public void OnActiveCameraRecordingStateChanged(ActiveCameraInfo a_camera, bool a_isNowRecording, TimeCode a_stateChangeTime)
		{
			if (a_isNowRecording)
			{
				if (m_shouldCreateNewShotOnRecord)
				{
					IngestDataShotVersionMeta targetMeta = m_targetShot?.DataSourcesTemplate.Clone() ?? new IngestDataShotVersionMeta();
					if (m_subscriber != null)
					{
						Logger.LogError("ShotVersionTemplate", "Expected target subscriber to be null, was not null. Did we miss a message?");
						m_subscriber.CameraTarget.CameraPropertyChanged -= m_subscriber.OnCameraPropertyChanged;
						m_subscriber = null;
					}

					m_subscriber = new CameraPropertyChangedSubscriber(targetMeta, a_camera);
					a_camera.CameraPropertyChanged += m_subscriber.OnCameraPropertyChanged;

					DataWranglerEventDelegates.Instance.NotifyRecordingStarted(a_camera, targetMeta);

					CreateNewShotVersion(targetMeta);
				}
			}
			else
			{
				if (m_subscriber != null)
				{
					if (m_subscriber.CameraTarget != a_camera)
					{
						throw new Exception("Multiple cameras?");
					}

					DataWranglerEventDelegates.Instance.NotifyRecordingFinished(a_camera, m_subscriber.Meta);

					a_camera.CameraPropertyChanged -= m_subscriber.OnCameraPropertyChanged;
					m_subscriber = null;
				}
			}
		}

		public void SetDisplayedShot(DataEntityShot? a_targetShot)
		{
			if (m_targetShot != null)
			{
				m_targetShot.DataSourcesTemplate.PropertyChanged -= OnDataSourcesChanged;
			}

			m_targetShot = a_targetShot;
			if (m_targetShot != null)
			{
				m_targetShot.DataSourcesTemplate.PropertyChanged += OnDataSourcesChanged;
			}

			TargetShotIngestData = m_targetShot?.DataSourcesTemplate;
		}

		private void OnDataSourcesChanged(object? a_sender, PropertyChangedEventArgs a_e)
		{
			if (m_targetShot != null)
			{
				Task<DataApiResponseGeneric> task = m_targetShot.ChangeTracker.CommitChanges(DataWranglerServiceProvider.Instance.TargetDataApi);
				FileSourcesFeedback.ProvideFeedback(task);
			}
		}
	}
}
