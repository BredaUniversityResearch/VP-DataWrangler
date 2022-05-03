using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using DataWranglerInterface.DebugSupport;
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

		private DebugWindow? m_debugWindow;

		public MainWindow()
		{
			InitializeComponent();
			if (Debugger.IsAttached)
			{
				m_debugWindow = new DebugWindow();
				m_debugWindow.Show();
			}

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

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			Content = null;
			if (m_shotRecordingPage != null)
			{
				m_shotRecordingPage.Dispose();
				m_shotRecordingPage = null;
			}

			if (m_debugWindow != null && m_debugWindow.IsVisible)
			{
				m_debugWindow.Close();
			}
		}
	}
}
