using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BlackmagicCameraControl;
using BlackmagicCameraControlBluetooth;
using CommonLogging;
using DataWranglerCommon;
using ShotGridIntegration;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for ShotRecordingPage.xaml
	/// </summary>
	public partial class ShotRecordingPage : Page, IDisposable
	{
		private BlackmagicBluetoothCameraAPIController m_bluetoothController;
		private ActiveCameraHandler m_activeCameraHandler;

		public delegate void ShotVersionCreationDelegate(int a_shotId);
		public event ShotVersionCreationDelegate? OnNewShotVersionCreationStarted;

		public delegate void ShotVersionCreatedDelegate(ShotGridEntityShotVersion a_data);
		public event ShotVersionCreatedDelegate? OnNewShotVersionCreated;

        public VideoPreviewControl? PreviewControl
        {
            set
            {
                m_activeCameraHandler.PreviewControl = value;
            }
        }

        public ShotRecordingPage()
		{
			InitializeComponent();
		
			m_bluetoothController = new BlackmagicBluetoothCameraAPIController();
			m_activeCameraHandler = new ActiveCameraHandler(m_bluetoothController);
			m_activeCameraHandler.OnCameraConnected += OnCameraConnected;
			m_activeCameraHandler.OnCameraDisconnected += OnCameraDisconnected;

			CameraInfoDebug.CameraApiController = m_bluetoothController;

			ProjectSelector.AsyncRefreshProjects();

			ProjectSelector.OnSelectedProjectChanged += OnSelectedProjectChanged;
			ShotSelector.OnSelectedShotChanged += OnSelectedShotChanged;
			CameraInfo.OnCameraRecordingStateChanged += ShotTemplateDisplay.OnActiveCameraRecordingStateChanged;

			ShotTemplateDisplay.SetParentControls(this, ProjectSelector, ShotSelector);
			ShotVersionInfoDisplay.SetParentControls(this);

			ShotSelector.OnNewShotCreatedButtonClicked += ShowShotCreationUI;
			ShotCreationControl.OnRequestCreateNewShot += OnRequestCreateNewShot;
		}

		public void Dispose()
		{
			m_activeCameraHandler.OnCameraConnected -= OnCameraConnected;
			m_activeCameraHandler.OnCameraDisconnected -= OnCameraDisconnected;
			m_bluetoothController.Dispose();
		}

		private void OnCameraConnected(ActiveCameraInfo a_camera)
		{
			CameraInfo.SetTargetCameraInfo(a_camera);
			CameraInfoDebug.SetTargetCamera(a_camera);
		}

		private void OnCameraDisconnected(ActiveCameraInfo a_handle)
		{
			if (CameraInfo.TargetCameraInfo == a_handle)
			{
				CameraInfo.SetTargetCameraInfo(null);
				CameraInfoDebug.SetTargetCamera(null);
			}
		}

		private void OnSelectedProjectChanged(int a_projectId, string a_projectName)
		{
			ShotSelector.AsyncRefreshShots(a_projectId);
		}

		private void OnSelectedShotChanged(ShotGridEntityShot? a_shotInfo)
		{
			ShotInfoDisplay.SetDisplayedShot(a_shotInfo);
			ShotVersionInfoDisplay.OnShotSelected(a_shotInfo?.Id ?? -1);
		}

		public void BeginAddShotVersion(int a_shotId)
		{
			OnNewShotVersionCreationStarted?.Invoke(a_shotId);
		}

		public void CompleteAddShotVersion(ShotGridEntityShotVersion a_data)
		{
			OnNewShotVersionCreated?.Invoke(a_data);
		}

		private void ShowShotCreationUI()
		{
			ShotCreationControl.Show();
		}

		private void OnRequestCreateNewShot(ShotGridEntityShot.ShotAttributes a_attributes)
		{
			int projectId = ProjectSelector.SelectedProjectId;
			ShotSelector.OnNewShotCreationStarted();
			DataWranglerServiceProvider.Instance.ShotGridAPI.CreateNewShot(projectId, a_attributes).ContinueWith(a_task =>
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
