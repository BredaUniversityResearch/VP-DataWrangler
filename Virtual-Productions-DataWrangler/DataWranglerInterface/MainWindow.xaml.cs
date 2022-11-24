using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using DataWranglerCommonWPF.Login;
using DataWranglerInterface.DebugSupport;
using DataWranglerInterface.Properties;
using DataWranglerInterface.ShotRecording;
using ShotGridIntegration;

namespace DataWranglerInterface
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		class SettingsCredentialProvider : ILoginCredentialProvider
		{
			public string OAuthRefreshToken
			{
				get => Settings.Default.OAuthRefreshToken;
				set
				{
					Settings.Default.OAuthRefreshToken = value;
					Settings.Default.Save();
				}
			}
		}

		private LoginPage m_loginPage;
		private ShotRecordingPage? m_shotRecordingPage;

		private DebugWindow? m_debugWindow;

		public MainWindow()
		{
			InitializeComponent();
			if (true)
			{
				m_debugWindow = new DebugWindow();
				m_debugWindow.Show();
			}

			m_loginPage = new LoginPage();
			OnRequestUserAuthentication();
		}

		private void OnLoggedIn()
		{
			Dispatcher.Invoke(() =>
			{
				m_shotRecordingPage = new ShotRecordingPage();
				Content = m_shotRecordingPage;
			});

			DataWranglerServiceProvider.Instance.ShotGridAPI.StartAutoRefreshToken(OnRequestUserAuthentication);
		}

		private void OnRequestUserAuthentication()
		{
			Dispatcher.InvokeAsync(() =>
			{
				m_loginPage.OnSuccessfulLogin += OnLoggedIn;
				m_loginPage.Initialize(DataWranglerServiceProvider.Instance.ShotGridAPI, new SettingsCredentialProvider());
				Content = m_loginPage;
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
