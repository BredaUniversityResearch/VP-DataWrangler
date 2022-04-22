using System.Windows.Controls;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for ShotRecordingPage.xaml
	/// </summary>
	public partial class ShotRecordingPage : Page
	{
		public ShotRecordingPage()
		{
			InitializeComponent();

			ProjectSelector.AsyncRefreshProjects();
		}
	}
}
