using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BlackmagicCameraControl;
using BlackmagicCameraControlBluetooth;
using CommonLogging;
using DataWranglerCommon;
using DataWranglerInterface.CameraHandling;
using ShotGridIntegration;

namespace DataWranglerInterface.ShotRecording
{
    /// <summary>
    /// Interaction logic for ShotRecordingPage.xaml
    /// </summary>
    public partial class ShotRecordingPage : Page, IDisposable
	{
		private BlackmagicBluetoothCameraAPIController? m_bluetoothController = null;
		private ActiveCameraHandler m_activeCameraHandler;

		public delegate void ShotVersionCreationDelegate(int a_shotId);
		public event ShotVersionCreationDelegate? OnNewShotVersionCreationStarted;

		public delegate void ShotVersionCreatedDelegate(ShotGridEntityShotVersion a_data);
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

			CameraInfoDebug.CameraApiController = m_bluetoothController;

			ProjectSelector.AsyncRefreshProjects();

			ProjectSelector.OnSelectedProjectChanged += OnSelectedProjectChanged;
			ShotSelector.OnSelectedShotChanged += OnSelectedShotChanged;
			CameraInfo.OnCameraRecordingStateChanged += ShotTemplateDisplay.OnActiveCameraRecordingStateChanged;

			ShotTemplateDisplay.SetParentControls(this, ProjectSelector, ShotSelector);
			ShotVersionInfoDisplay.SetParentControls(this);

			ShotSelector.OnNewShotCreatedButtonClicked += ShowShotCreationUI;
			ShotCreationControl.OnRequestCreateNewShot += OnRequestCreateNewShot;

			DataWranglerServiceProvider.Instance.EventDelegates.OnRecordingStarted += OnRecordingStarted;
			DataWranglerServiceProvider.Instance.EventDelegates.OnRecordingFinished+= OnRecordingFinished;
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
			CameraInfoDebug.SetTargetCamera(a_camera);
		}

		private void OnCameraDisconnected(ActiveCameraInfo a_handle)
		{
			CameraInfo.RemoveTargetCameraInfo(a_handle);
			CameraInfoDebug.SetTargetCamera(null);
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

		private void OnRequestCreateNewShot(ShotGridEntityShotAttributes a_gridEntityShotAttributes)
		{
			int projectId = ProjectSelector.SelectedProjectId;
			ShotSelector.OnNewShotCreationStarted();
			DataWranglerServiceProvider.Instance.ShotGridAPI.CreateNewShot(projectId, a_gridEntityShotAttributes).ContinueWith(a_task =>
			{
				if (a_task.Result.IsError)
				{
					Logger.LogError("ShotRecording", $"Failed to create new shot, error response: {a_task.Result.ErrorInfo}");
				}
				ShotSelector.OnNewShotCreationFinished(a_task.Result.ResultData);
			});
		}

		private void OnRecordingStarted(DataWranglerShotVersionMeta a_shotMetaData)
		{
			DataWranglerFileSourceMetaViconTrackingData? trackingDataMeta = a_shotMetaData.FindFileSourceMeta<DataWranglerFileSourceMetaViconTrackingData>();
			if (trackingDataMeta != null)
			{
				if (DataWranglerServiceProvider.Instance.ShogunLiveService.StartCapture(trackingDataMeta.TempCaptureFileName, trackingDataMeta.TempCaptureLibraryPath, out var task))
				{
					task.ContinueWith((a_result) => {
						if (!a_result.Result)
						{
							Logger.LogError("ShotRecording", $"Vicon failed to send a confirmation that recording of library " +
							                                 $"{trackingDataMeta.TempCaptureLibraryPath} with file name {trackingDataMeta.TempCaptureFileName} started");
						}
					});
				}
				else
				{
					Logger.LogError("ShotRecording", "Failed to start shogun live recording.");
				}
			}
		}

		private void OnRecordingFinished(DataWranglerShotVersionMeta a_shotMetaData)
		{
			DataWranglerFileSourceMetaViconTrackingData? trackingDataMeta = a_shotMetaData.FindFileSourceMeta<DataWranglerFileSourceMetaViconTrackingData>();
			if (trackingDataMeta != null)
			{
				if (!DataWranglerServiceProvider.Instance.ShogunLiveService.StopCapture(true, trackingDataMeta.TempCaptureFileName, trackingDataMeta.TempCaptureLibraryPath))
				{
					Logger.LogError("ShotRecording", $"Failed to stop shogun capture of file {trackingDataMeta.TempCaptureFileName} in library {trackingDataMeta.TempCaptureLibraryPath}");
				}
			}
		}
	}
}
