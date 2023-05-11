using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using DataWranglerCommonWPF.Login;
using DataWranglerInterface.CameraPreview;
using DataWranglerInterface.DebugSupport;
using DataWranglerInterface.Properties;
using DataWranglerInterface.ShotRecording;

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
        private CameraPreviewWindow? m_previewWindow;

		public MainWindow()
		{
			InitializeComponent();
			if (Debugger.IsAttached)
			{
				m_debugWindow = new DebugWindow();
				m_debugWindow.Show();
			}

            m_previewWindow = new CameraPreviewWindow();
			m_previewWindow.Show();

            m_loginPage = new LoginPage();
			OnRequestUserAuthentication();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.IsDown && e.Key == Key.F1)
			{
				if (m_debugWindow == null || !m_debugWindow.IsVisible)
				{
					m_debugWindow = new DebugWindow();
					m_debugWindow.Show();
				}
			}
		}

		private void OnLoggedIn()
		{
			Dispatcher.Invoke(() =>
			{
				m_shotRecordingPage = new ShotRecordingPage();
                if (m_previewWindow != null)
                {
                    m_shotRecordingPage.PreviewControl = m_previewWindow.PreviewControl;
                }
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

			if (m_previewWindow != null && m_previewWindow.IsVisible)
			{
				m_previewWindow.Close();
			}
		}

		private void OnWindowMouseDown(object a_sender, MouseButtonEventArgs a_e)
		{
			FocusManager.SetFocusedElement(this, null);
			Keyboard.ClearFocus();
		}
	}
}
