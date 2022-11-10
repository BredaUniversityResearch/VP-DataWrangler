using System.Windows;
using System.Windows.Controls;
using BlackmagicCameraControl;
using ShotGridIntegration;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for ShotRecordingPage.xaml
	/// </summary>
	public partial class ShotRecordingPage : Page, IDisposable
	{
		private BlackmagicBluetoothCameraAPIController m_apiController;
		private ActiveCameraHandler m_activeCameraHandler;

		public ShotRecordingPage()
		{
			InitializeComponent();
		
			m_apiController = new BlackmagicBluetoothCameraAPIController();
			m_activeCameraHandler = new ActiveCameraHandler(m_apiController);
			m_activeCameraHandler.OnCameraConnected += OnCameraConnected;
			m_activeCameraHandler.OnCameraDisconnected += OnCameraDisconnected;

			CameraInfoDebug.CameraApiController = m_apiController;

			ProjectSelector.AsyncRefreshProjects();

			ProjectSelector.OnSelectedProjectChanged += OnSelectedProjectChanged;
			ShotSelector.OnSelectedShotChanged += OnSelectedShotChanged;
			CameraInfo.OnCameraRecordingStateChanged += ShotVersionDisplay.OnActiveCameraRecordingStateChanged;

			ShotVersionDisplay.SetProjectSelector(ProjectSelector);
		}

		public void Dispose()
		{
			m_activeCameraHandler.OnCameraConnected -= OnCameraConnected;
			m_activeCameraHandler.OnCameraDisconnected -= OnCameraDisconnected;
			m_apiController.Dispose();
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
			ShotVersionDisplay.OnShotSelected(a_shotInfo?.Id ?? -1);
		}
	}
}
