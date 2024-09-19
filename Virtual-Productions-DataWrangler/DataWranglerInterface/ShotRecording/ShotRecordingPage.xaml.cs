using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using CameraControlOverEthernet.CameraControl;
using CommonLogging;
using DataApiCommon;
using DataWranglerCommon;
using DataWranglerCommon.CameraHandling;
using DataWranglerCommon.IngestDataSources;
using DataWranglerInterface.CameraHandling;

namespace DataWranglerInterface.ShotRecording
{
    /// <summary>
    /// Interaction logic for ShotRecordingPage.xaml
    /// </summary>
    public partial class ShotRecordingPage : Page, IDisposable
	{
		private ActiveCameraHandler m_activeCameraHandler;
		private IngestDataSourceHandlerCollection m_ingestDataHandler = new IngestDataSourceHandlerCollection();

		public delegate void ShotVersionCreationDelegate(Guid a_shotId);
		public event ShotVersionCreationDelegate? OnNewShotVersionCreationStarted;

		public delegate void ShotVersionCreatedDelegate(DataEntityShotVersion a_data);
		public event ShotVersionCreatedDelegate? OnNewShotVersionCreated;

		private ShotRecordingApplicationState m_shotRecordingState = new ShotRecordingApplicationState();
		private ShotRecordingStateTransmitter m_shotRecordingStateTransmitter;

        public VideoPreviewControl? PreviewControl
        {
            set => m_activeCameraHandler.PreviewControl = value;
        }

        public ShotRecordingPage(EthernetRelayCameraController? a_cameraController)
		{
			InitializeComponent();

			m_activeCameraHandler = new ActiveCameraHandler(a_cameraController);
			m_activeCameraHandler.OnVirtualCameraConnected += VirtualCameraConnected;
			m_activeCameraHandler.OnCameraDisconnected += OnCameraDisconnected;

			//CameraInfoDebug.CameraApiController = m_bluetoothController;

			m_shotRecordingState.PropertyChanged += OnRecordingStatePropertyChanged;
			
			ProjectSelector.AsyncRefreshProjects();

			ProjectSelector.OnSelectedProjectChanged += (a_selectedProject) => m_shotRecordingState.SelectedProject = a_selectedProject;
			ShotSelector.OnSelectedShotChanged += (a_selectedShot) => m_shotRecordingState.SelectedShot = a_selectedShot;

			CameraInfo.OnCameraRecordingStateChanged += ShotTemplateDisplay.OnActiveCameraRecordingStateChanged;

			ShotTemplateDisplay.SetParentControls(this, m_shotRecordingState);
			ShotVersionInfoDisplay.SetParentControls(this, m_shotRecordingState);

			ShotSelector.OnNewShotCreatedButtonClicked += ShowShotCreationUI;
			ShotCreationControl.OnRequestCreateNewShot += OnRequestCreateNewShot;

			m_ingestDataHandler.CreateAvailableHandlers(DataWranglerEventDelegates.Instance, DataWranglerServiceProvider.Instance);

			m_shotRecordingStateTransmitter = new ShotRecordingStateTransmitter(m_shotRecordingState);

			if (a_cameraController != null)
			{
				a_cameraController.ReReportAllCameras();
			}
		}

		public void Dispose()
		{
			m_activeCameraHandler.OnVirtualCameraConnected -= VirtualCameraConnected;
			m_activeCameraHandler.OnCameraDisconnected -= OnCameraDisconnected;
		}

		private void VirtualCameraConnected(ActiveCameraInfo a_camera)
		{
			CameraInfo.AddTargetCameraInfo(a_camera);
			//CameraInfoDebug.SetTargetCamera(a_camera);
		}

		private void OnCameraDisconnected(ActiveCameraInfo a_handle)
		{
			CameraInfo.RemoveTargetCameraInfo(a_handle);
			//CameraInfoDebug.SetTargetCamera(null);
		}

		private void OnRecordingStatePropertyChanged(object? a_sender, PropertyChangedEventArgs a_e)
		{
			ShotRecordingApplicationState? appState = (ShotRecordingApplicationState?) a_sender;
			if (appState == null)
			{
				throw new Exception();
			}

			if (a_e.PropertyName == nameof(ShotRecordingApplicationState.SelectedProject))
			{
				OnSelectedProjectChanged(appState.SelectedProject);
			}
			else if (a_e.PropertyName == nameof(ShotRecordingApplicationState.SelectedShot))
			{
				OnSelectedShotChanged(appState.SelectedShot);
			}
		}

		private void OnSelectedProjectChanged(DataEntityProject? a_project)
		{
			if (a_project != null)
			{
				ShotSelector.AsyncRefreshShots(a_project.EntityId);
			}
		}

		private void OnSelectedShotChanged(DataEntityShot? a_shotInfo)
		{
			ShotInfoDisplay.SetDisplayedShot(a_shotInfo);
			ShotTemplateDisplay.SetDisplayedShot(a_shotInfo);
		}

		public void BeginAddShotVersion(Guid a_shotId)
		{
			OnNewShotVersionCreationStarted?.Invoke(a_shotId);
		}

		public void CompleteAddShotVersion(DataEntityShotVersion a_data)
		{
			OnNewShotVersionCreated?.Invoke(a_data);
		}

		private void ShowShotCreationUI()
		{
			ShotCreationControl.Show();
		}

		private void OnRequestCreateNewShot(DataEntityShot a_newShotAttributes)
		{
			Guid projectId = ProjectSelector.SelectedProjectId;

			if (ShotInfoDisplay.DisplayedShot != null)
			{
				a_newShotAttributes.DataSourcesTemplate = ShotInfoDisplay.DisplayedShot.DataSourcesTemplate.Clone();
			}

			ShotSelector.OnNewShotCreationStarted();
			DataWranglerServiceProvider.Instance.TargetDataApi.CreateNewShot(projectId, a_newShotAttributes).ContinueWith(a_task =>
			{
				if (a_task.Result.IsError)
				{
					Logger.LogWarning("ShotRecording", $"Failed to create new shot, error response: {a_task.Result.ErrorInfo}");
					Dispatcher.InvokeAsync(() => ShotCreationControl.OnNewShotCreationFailed(a_newShotAttributes, a_task.Result.ErrorInfo.ToString()));
				}
				ShotSelector.OnNewShotCreationFinished(a_task.Result.ResultData);
			});
		}

		private void OpenProjectFolderButton_OnClick(object a_sender, RoutedEventArgs a_e)
		{
			Guid projectId = ProjectSelector.SelectedProjectId;
			DataWranglerServiceProvider.Instance.TargetDataApi.GetBrowsableLocalStoragePathForProject(projectId).ContinueWith(a_task => 
			{
				if (!a_task.Result.IsError)
				{
					string localPath = a_task.Result.ResultData.LocalPath;
					Process.Start(new ProcessStartInfo() { 
							FileName = localPath + Path.DirectorySeparatorChar,
							UseShellExecute = true,
							Verb = "open"
						});
				}
				else
				{
					Logger.LogError("ShotRecording", $"Failed to browse to project data store. API call returned: {a_task.Result.ErrorInfo}");
				}
			});
		}
	}
}
