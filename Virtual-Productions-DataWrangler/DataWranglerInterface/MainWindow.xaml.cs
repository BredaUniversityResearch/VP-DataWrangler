using System.Windows;
using DataWranglerInterface.Login;
using DataWranglerInterface.ShotRecording;

namespace DataWranglerInterface
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private LoginPage? m_loginPage;
		private ShotRecordingPage? m_shotRecordingPage;

		public MainWindow()
		{
			InitializeComponent();

			m_loginPage = new LoginPage();
			m_loginPage.OnSuccessfulLogin += OnLoggedIn;
			Content = m_loginPage;
		}

		private void OnLoggedIn()
		{
			Dispatcher.Invoke(() =>
			{
				m_shotRecordingPage = new ShotRecordingPage();
				Content = m_shotRecordingPage;
				m_loginPage = null;
			});
		}
	}
}
