using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using CommonLogging;
using DataWranglerCommon;
using DataWranglerInterface.CameraHandling;
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
			public DataWranglerShotVersionMeta Meta;
			public ActiveCameraInfo CameraTarget;

			public CameraPropertyChangedSubscriber(DataWranglerShotVersionMeta a_meta, ActiveCameraInfo a_cameraInfo)
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

				foreach (DataWranglerFileSourceMeta source in Meta.FileSources)
				{
					if (source is DataWranglerFileSourceMetaBlackmagicUrsa ursaSource)
					{ 
						ursaSource.CodecName = CameraTarget.SelectedCodec;
					}
				}
			}
		};

		private ShotRecordingPage? m_parentPage = null;

		private ProjectSelectorControl? m_projectSelector = null;
		private ShotSelectorControl? m_shotSelectorControl = null;

		private bool m_shouldCreateNewShotOnRecord = true;

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

		private void CreateNewShotVersion(DataWranglerShotVersionMeta a_meta)
		{
			if (m_shotSelectorControl == null || m_projectSelector == null)
			{
				throw new Exception();
			}

			int targetShotId = m_shotSelectorControl.SelectedShotId;
			if (targetShotId != -1)
			{
				m_parentPage?.BeginAddShotVersion(targetShotId);

				DataWranglerServiceProvider.Instance.ShotGridAPI.GetVersionsForShot(targetShotId, new ShotGridSortSpecifier("code", false)).ContinueWith(a_task => {
					if (!a_task.Result.IsError)
					{
						int nextTakeId = FindNextTakeIdFromShotVersions(a_task.Result.ResultData) + 1;

						ConfigStringBuilder sb = new ConfigStringBuilder();
						sb.AddReplacement("ShotVersionId", nextTakeId.ToString());

						ShotVersionAttributes attributes = new ShotVersionAttributes();
						attributes.VersionCode = Configuration.DataWranglerConfig.Instance.ShotVersionNameTemplate.Build(sb);
						attributes.DataWranglerMeta =
							JsonConvert.SerializeObject(a_meta, DataWranglerSerializationSettings.Instance);

						DataWranglerServiceProvider.Instance.ShotGridAPI.CreateNewShotVersion(
							m_projectSelector.SelectedProjectId, targetShotId,
							attributes).ContinueWith(a_result =>
						{
							if (!a_result.Result.IsError)
							{
								m_parentPage?.CompleteAddShotVersion(a_result.Result.ResultData);
                                DataWranglerServiceProvider.Instance.EventDelegates.NotifyShotCreated(a_result.Result
                                    .ResultData.Id);
                            }
						});
					}
				});
			}
		}

		private int FindNextTakeIdFromShotVersions(ShotGridEntityShotVersion[] a_resultData)
		{
			ConfigStringBuilder sb = new ConfigStringBuilder();
			sb.AddReplacement("ShotVersionId", "([0-9]{2})");
			Regex shotNameTemplateMatcher = new Regex(Configuration.DataWranglerConfig.Instance.ShotVersionNameTemplate.Build(sb));

			int nextShotId = 0;
			foreach (ShotGridEntityShotVersion shotVersion in a_resultData)
			{
				Match nameMatch = shotNameTemplateMatcher.Match(shotVersion.Attributes.VersionCode);
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
					DataWranglerShotVersionMeta targetMeta = VersionTemplateFileSourcesControl.CreateMetaFromCurrentTemplate();
					if (m_subscriber != null)
					{
						Logger.LogError("ShotVersionTemplate", "Expected target subscriber to be null, was not null. Did we miss a message?");
						m_subscriber.CameraTarget.CameraPropertyChanged -= m_subscriber.OnCameraPropertyChanged;
						m_subscriber = null;
					}

					m_subscriber = new CameraPropertyChangedSubscriber(targetMeta, a_camera);
					a_camera.CameraPropertyChanged += m_subscriber.OnCameraPropertyChanged;

					foreach (DataWranglerFileSourceMeta source in targetMeta.FileSources)
					{
						if (source is DataWranglerFileSourceMetaBlackmagicUrsa ursaSource)
                        {
                            ursaSource.ApplyCameraProperties(DateTimeOffset.UtcNow, a_camera.CurrentTimeCode,
                                a_camera.SelectedCodec);
                        }

                        source.OnRecordingStarted(a_stateChangeTime);
                    } 

					DataWranglerServiceProvider.Instance.EventDelegates.NotifyRecordingStarted(targetMeta);

					CreateNewShotVersion(targetMeta);
				}
			}
			else
			{
				if (m_subscriber != null)
				{
					foreach (DataWranglerFileSourceMeta meta in m_subscriber.Meta.FileSources)
					{
						meta.OnRecordingStopped();
					}

					if (m_subscriber.CameraTarget != a_camera)
					{
						throw new Exception("Multiple cameras?");
					}

					DataWranglerServiceProvider.Instance.EventDelegates.NotifyRecordingFinished(m_subscriber.Meta);

					a_camera.CameraPropertyChanged -= m_subscriber.OnCameraPropertyChanged;
					m_subscriber = null;
				}
			}
		}
	}
}
