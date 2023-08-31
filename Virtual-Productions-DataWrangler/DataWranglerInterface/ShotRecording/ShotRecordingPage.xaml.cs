using System.Windows.Controls;
using BlackmagicCameraControlBluetooth;
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
		private BlackmagicBluetoothCameraAPIController? m_bluetoothController = null;
		private ActiveCameraHandler m_activeCameraHandler;
		private IngestDataSourceHandlerCollection m_ingestDataHandler = new IngestDataSourceHandlerCollection();

		public delegate void ShotVersionCreationDelegate(Guid a_shotId);
		public event ShotVersionCreationDelegate? OnNewShotVersionCreationStarted;

		public delegate void ShotVersionCreatedDelegate(DataEntityShotVersion a_data);
		public event ShotVersionCreatedDelegate? OnNewShotVersionCreated;

        public VideoPreviewControl? PreviewControl
        {
            set => m_activeCameraHandler.PreviewControl = value;
        }

        public ShotRecordingPage()
		{
			InitializeComponent();
		
			m_activeCameraHandler = new ActiveCameraHandler(m_bluetoothController);
			m_activeCameraHandler.OnVirtualCameraConnected += VirtualCameraConnected;
			m_activeCameraHandler.OnCameraDisconnected += OnCameraDisconnected;

			//CameraInfoDebug.CameraApiController = m_bluetoothController;

			ProjectSelector.AsyncRefreshProjects();

			ProjectSelector.OnSelectedProjectChanged += OnSelectedProjectChanged;
			ShotSelector.OnSelectedShotChanged += OnSelectedShotChanged;
			CameraInfo.OnCameraRecordingStateChanged += ShotTemplateDisplay.OnActiveCameraRecordingStateChanged;

			ShotTemplateDisplay.SetParentControls(this, ProjectSelector, ShotSelector);
			ShotVersionInfoDisplay.SetParentControls(this);

			ShotSelector.OnNewShotCreatedButtonClicked += ShowShotCreationUI;
			ShotCreationControl.OnRequestCreateNewShot += OnRequestCreateNewShot;

			m_ingestDataHandler.CreateAvailableHandlers(DataWranglerEventDelegates.Instance, DataWranglerServiceProvider.Instance);
		}

        public void Dispose()
		{
			m_activeCameraHandler.OnVirtualCameraConnected -= VirtualCameraConnected;
			m_activeCameraHandler.OnCameraDisconnected -= OnCameraDisconnected;
			m_bluetoothController?.Dispose();
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

		private void OnSelectedProjectChanged(Guid a_projectId, string a_projectName)
		{
			ShotSelector.AsyncRefreshShots(a_projectId);
		}

		private void OnSelectedShotChanged(DataEntityShot? a_shotInfo)
		{
			ShotInfoDisplay.SetDisplayedShot(a_shotInfo);
			ShotVersionInfoDisplay.OnShotSelected(a_shotInfo?.EntityId ?? Guid.Empty);
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

		private void OnRequestCreateNewShot(DataEntityShot a_gridEntityShotAttributes)
		{
			Guid projectId = ProjectSelector.SelectedProjectId;
			ShotSelector.OnNewShotCreationStarted();
			DataWranglerServiceProvider.Instance.TargetDataApi.CreateNewShot(projectId, a_gridEntityShotAttributes).ContinueWith(a_task =>
			{
				if (a_task.Result.IsError)
				{
					Logger.LogError("ShotRecording", $"Failed to create new shot, error response: {a_task.Result.ErrorInfo}");
				}
				ShotSelector.OnNewShotCreationFinished(a_task.Result.ResultData);
			});
		}
	}
}
