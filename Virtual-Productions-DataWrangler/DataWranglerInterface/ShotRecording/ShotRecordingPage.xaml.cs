using System.Windows;
using System.Windows.Controls;
using BlackmagicCameraControl;

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
		}

		public void Dispose()
		{
			m_controller.Dispose();
		}
	}
}
