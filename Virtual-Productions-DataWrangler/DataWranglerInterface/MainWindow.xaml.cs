using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using DataApiCommon;
using DataApiSFTP;
using DataWranglerCommon;
using DataWranglerCommon.ShogunLiveSupport;
using DataWranglerCommonWPF.Login;
using DataWranglerInterface.CameraPreview;
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

		private ShotGridLoginPage? m_shotGridLoginPage = null;
		private ShotRecordingPage? m_shotRecordingPage;

		private DebugWindow? m_debugWindow;
        private CameraPreviewWindow? m_previewWindow;

        private DataApi m_targetAPI = new DataApiSFTPFileSystem();
        private ShogunLiveService m_shogunService = new ShogunLiveService(30);

        public MainWindow()
		{
			InitializeComponent();

			DataWranglerServices services = new DataWranglerServices(m_targetAPI, m_shogunService);
			DataWranglerServiceProvider.Use(services);	

			if (Debugger.IsAttached)
			{
				m_debugWindow = new DebugWindow();
				m_debugWindow.Show();
			}

            //m_previewWindow = new CameraPreviewWindow();
			//m_previewWindow.Show();

			if (m_targetAPI is ShotGridAPI)
			{
				m_shotGridLoginPage = new ShotGridLoginPage();
				OnRequestUserAuthentication();
			}
			else if (m_targetAPI is DataApiSFTPFileSystem fsApi)
			{
				if (fsApi.Connect(DataApiSFTPConfig.DefaultConfig))
				{
					OnLoggedIn();
				}
			}
			else
			{
				throw new Exception("Unknown api backend");
			}
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.IsDown)
			{
				if (e.Key == Key.F1)
				{
					if (m_debugWindow == null || !m_debugWindow.IsVisible)
					{
						m_debugWindow = new DebugWindow();
						m_debugWindow.Show();
					}
				}
				else if (e.Key == Key.F2)
				{
					m_previewWindow = new CameraPreviewWindow();
					m_previewWindow.Show();
					if (m_shotRecordingPage != null)
					{
						m_shotRecordingPage.PreviewControl = m_previewWindow.PreviewControl;
					}
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

			if (m_targetAPI is ShotGridAPI sgApi)
			{
				sgApi.StartAutoRefreshToken(OnRequestUserAuthentication);
			}
		}

		private void OnRequestUserAuthentication()
		{
			if (m_targetAPI is ShotGridAPI sgApi)
			{
				Dispatcher.InvokeAsync(() =>
				{
					if (m_shotGridLoginPage == null)
					{
						throw new Exception("Expected login page to be here");
					}

					m_shotGridLoginPage.OnSuccessfulLogin += OnLoggedIn;
					m_shotGridLoginPage.Initialize(sgApi, new SettingsCredentialProvider());
					Content = m_shotGridLoginPage;
				});
			}
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
