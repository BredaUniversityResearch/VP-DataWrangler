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
		private ShotRecordingApplicationState? m_shotRecordingState = null;

		private DataEntityShot? m_targetShot = null;
		
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

		public void SetParentControls(ShotRecordingPage a_parentPage, ShotRecordingApplicationState a_applicationState)
		{
			m_parentPage = a_parentPage;
			m_shotRecordingState = a_applicationState;
		}

		private void CreateNewShotVersion(IngestDataShotVersionMeta a_meta)
		{
			if (m_shotRecordingState == null)
			{
				throw new Exception();
			}

			Guid targetShotId = m_shotRecordingState.SelectedShot?.EntityId ?? Guid.Empty;
			Guid targetProjectId = m_shotRecordingState.SelectedProject?.EntityId ?? Guid.Empty;
			if (targetShotId != Guid.Empty && targetProjectId != Guid.Empty)
			{
				m_parentPage?.BeginAddShotVersion(targetShotId);
				DataWranglerServiceProvider.Instance.TargetDataApi.GetVersionsForShot(targetShotId, (a_lhs, a_rhs) => string.Compare(a_lhs.ShotVersionName, a_rhs.ShotVersionName, StringComparison.Ordinal)).ContinueWith(a_fetchTask => {
					if (!a_fetchTask.Result.IsError)
					{
						int nextTakeId = FindHighestShotVersionIdFromShotVersions(a_fetchTask.Result.ResultData) + 1;

						ConfigStringBuilder sb = new ConfigStringBuilder();
						sb.AddReplacement("ShotVersionId", nextTakeId.ToString("D2"));

						DataEntityShotVersion newVersion = new DataEntityShotVersion
						{
							ShotVersionName = Configuration.DataWranglerInterfaceConfig.Instance.ShotVersionNameTemplate.Build(sb),
							DataWranglerMeta = JsonConvert.SerializeObject(a_meta, DataWranglerSerializationSettings.Instance)
						};

						DataWranglerServiceProvider.Instance.TargetDataApi.CreateNewShotVersion(targetProjectId, targetShotId, newVersion)
							.ContinueWith(a_result =>
						{
							if (!a_result.Result.IsError)
							{
								m_parentPage?.CompleteAddShotVersion(a_result.Result.ResultData);
                                DataWranglerEventDelegates.Instance.NotifyShotCreated(a_result.Result
                                    .ResultData.EntityId);
                            }
							else
							{
								Logger.LogError("Ingestinator", $"Failed to create shot with name {newVersion.ShotVersionName}. Api reported failure: {a_result.Result.ErrorInfo}");
							}
						});
					}
					else
					{
						Logger.LogError("Ingestinator", $"Failed to fetch versions for shot {targetShotId}. Api reported failure: {a_fetchTask.Result.ErrorInfo}");
					}
				});
			}
		}

		private int FindHighestShotVersionIdFromShotVersions(DataEntityShotVersion[] a_resultData)
		{
			ConfigStringBuilder sb = new ConfigStringBuilder();
			sb.AddReplacement("ShotVersionId", "([0-9]{2})");
			Regex shotNameTemplateMatcher = new Regex(Configuration.DataWranglerInterfaceConfig.Instance.ShotVersionNameTemplate.Build(sb));
			
			int nextShotId = 0;
			foreach (DataEntityShotVersion shotVersion in a_resultData)
			{
				Match nameMatch = shotNameTemplateMatcher.Match(shotVersion.ShotVersionName);
				if (nameMatch.Success && nameMatch.Groups.Count >= 1)
				{
					int parsedShotVersionId = int.Parse(nameMatch.Groups[1].ValueSpan);
					nextShotId = Math.Max(nextShotId, parsedShotVersionId);
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
				m_targetShot.ChangeTracker.OnChangeApplied -= OnDataSourcesChanged;
			}

			m_targetShot = a_targetShot;
			if (m_targetShot != null)
			{
				m_targetShot.ChangeTracker.OnChangeApplied += OnDataSourcesChanged;
			}

			TargetShotIngestData = m_targetShot?.DataSourcesTemplate;
		}

		private void OnDataSourcesChanged(object? a_sender, PropertyChangedEventArgs a_e)
		{
			if (m_targetShot != null)
			{
				if (m_targetShot == a_sender && a_e.PropertyName == nameof(m_targetShot.DataSourcesTemplate))
				{
					Task<DataApiResponseGeneric> task = m_targetShot.ChangeTracker.CommitChanges(DataWranglerServiceProvider.Instance.TargetDataApi);
					FileSourcesFeedback.ProvideFeedback(task);
				}
			}
		}
	}
}
