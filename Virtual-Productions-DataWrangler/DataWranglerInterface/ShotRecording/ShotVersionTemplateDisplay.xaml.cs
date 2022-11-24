using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using DataWranglerCommon;
using Newtonsoft.Json;
using ShotGridIntegration;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for ShotVersionTemplateDisplay.xaml
	/// </summary>
	public partial class ShotVersionTemplateDisplay : UserControl
	{
		private const string ShotNameTemplate = "Take {0:D2}";
		private static readonly Regex ShotNameTemplateMatcher = new Regex("Take ([0-9]{2})");

		private ShotRecordingPage? m_parentPage = null;

		private ProjectSelectorControl? m_projectSelector = null;
		private ShotSelectorControl? m_shotSelectorControl = null;
		private DataWranglerShotVersionMeta m_currentVersionMeta = new DataWranglerShotVersionMeta();

		private bool m_shouldCreateNewShotOnRecord = true;

		public ShotVersionTemplateDisplay()
		{
			InitializeComponent();
			
			CreateNewTake.Click += OnCreateNewShotVersion;

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

		private void OnCreateNewShotVersion(object a_sender, RoutedEventArgs a_e)
		{
			CreateNewShot(new DataWranglerShotVersionMeta());
		}

		private void CreateNewShot(DataWranglerShotVersionMeta a_meta)
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

						ShotGridEntityShotVersion.ShotVersionAttributes attributes =
							new ShotGridEntityShotVersion.ShotVersionAttributes();
						attributes.VersionCode = string.Format(ShotNameTemplate, nextTakeId);
						attributes.DataWranglerMeta =
							JsonConvert.SerializeObject(a_meta, DataWranglerSerializationSettings.Instance);

						DataWranglerServiceProvider.Instance.ShotGridAPI.CreateNewShotVersion(
							m_projectSelector.SelectedProjectId, targetShotId,
							attributes).ContinueWith(a_result =>
						{
							if (!a_result.Result.IsError)
							{
								m_parentPage?.CompleteAddShotVersion(a_result.Result.ResultData);
							}
						});
					}
				});
			}
		}

		private int FindNextTakeIdFromShotVersions(ShotGridEntityShotVersion[] a_resultData)
		{
			int nextShotId = 1;
			foreach (ShotGridEntityShotVersion shotVersion in a_resultData)
			{
				Match nameMatch = ShotNameTemplateMatcher.Match(shotVersion.Attributes.VersionCode);
				if (nameMatch.Success && nameMatch.Groups.Count >= 1)
				{
					nextShotId = int.Parse(nameMatch.Groups[1].ValueSpan);
					break;
				}
			}

			return nextShotId;
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

				foreach (DataWranglerFileSourceMeta source in targetMeta.FileSources)
				{
					if (source is DataWranglerFileSourceMetaBlackmagicUrsa ursaSource)
					{
						ursaSource.RecordingStart = a_stateChangeTime;
						ursaSource.StorageTarget = a_camera.CurrentStorageTarget;
						ursaSource.CodecName = a_camera.SelectedCodec;
					}
				}

				if (m_shouldCreateNewShotOnRecord)
				{
					CreateNewShot(targetMeta);
				}
			}
		}
	}
}
