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
		private BlackmagicCameraController m_controller;

		public ShotRecordingPage()
		{
			InitializeComponent();
		
			m_controller = new BlackmagicCameraController();
			CameraInfo.SetController(m_controller);

			ProjectSelector.AsyncRefreshProjects();

			ProjectSelector.OnSelectedProjectChanged += OnSelectedProjectChanged;
			ShotSelector.OnSelectedShotChanged += OnSelectedShotChanged;
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

		public void Dispose()
		{
			m_controller.Dispose();
		}
	}
}
