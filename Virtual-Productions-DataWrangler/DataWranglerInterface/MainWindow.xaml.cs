using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Input;
using CommonLogging;
using DataApiCommon;
using DataApiSFTP;
using DataWranglerCommon;
using DataWranglerCommon.ShogunLiveSupport;
using DataWranglerCommonWPF.Login;
using DataWranglerInterface.CameraPreview;
using DataWranglerInterface.Configuration;
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

		private APIConnectionPage? m_apiConnectionPage = null;
		private ShotRecordingPage? m_shotRecordingPage;

		private ApplicationLogDisplayWindow? m_debugWindow;
        private CameraPreviewWindow? m_previewWindow;

        private readonly DataApi m_targetAPI = new DataApiSFTPFileSystem(DataApiSFTPConfig.DefaultConfig);
        private readonly ShogunLiveService m_shogunService = new ShogunLiveService(30);

        public MainWindow()
		{
			InitializeComponent();

			TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
			Logger.Instance.OnMessageLogged += LoggerMessageLogged;

			DataWranglerInterfaceConfig.Use(new DataWranglerInterfaceConfig());
			DataWranglerServices services = new DataWranglerServices(m_targetAPI, m_shogunService);
			DataWranglerServiceProvider.Use(services);	

			if (Debugger.IsAttached)
			{
				m_debugWindow = new ApplicationLogDisplayWindow();
				m_debugWindow.Show();
			}

            //m_previewWindow = new CameraPreviewWindow();
			//m_previewWindow.Show();

			m_targetAPI.StartConnect().ContinueWith(a_resultTask =>
			{
				if (a_resultTask.Result)
				{
					OnLoggedIn();
				}
				else
				{
					m_apiConnectionPage?.OnConnectFailure("Failed to connect to API");
				}
			});
			OnApiConnectStarted();
		}

        private void LoggerMessageLogged(TimeOnly a_time, string a_source, ELogSeverity a_severity, string a_message)
        {
	        if (a_severity == ELogSeverity.Error)
	        {
		        Dispatcher.InvokeAsync(() =>
		        {
			        if (m_debugWindow == null || !m_debugWindow.IsVisible)
			        {
				        m_debugWindow = new ApplicationLogDisplayWindow();
				        m_debugWindow.Show();
			        }
		        });
	        }
        }

        private void TaskSchedulerOnUnobservedTaskException(object? a_sender, UnobservedTaskExceptionEventArgs a_e)
        {
	        StringBuilder sb = new StringBuilder();
	        sb.AppendLine("A task faulted with unobserved exception(s).");
	        ReadOnlyCollection<Exception> innerExceptions = a_e.Exception.Flatten().InnerExceptions;
	        for (int i = 0; i < innerExceptions.Count; ++i)
	        {
		        Exception ex = innerExceptions[i];
		        sb.AppendLine($"Exception {i} ( {ex.GetType()} ):");
		        sb.AppendLine($"\t{ex.Message}\n{ex.StackTrace}");
	        }

	        Logger.LogError("Task", sb.ToString());
        }

        private void OnApiConnectStarted()
        {
			Dispatcher.InvokeAsync(() =>
			{
				m_apiConnectionPage = new APIConnectionPage();
				Content = m_apiConnectionPage;
			});

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
						m_debugWindow = new ApplicationLogDisplayWindow();
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

			//if (m_targetAPI is ShotGridAPI sgApi)
			//{
			//	sgApi.StartAutoRefreshToken(OnRequestUserAuthentication);
			//}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			DataWranglerInterfaceConfig.Instance.Save();

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
