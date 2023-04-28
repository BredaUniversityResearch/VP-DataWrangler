using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using DataWranglerCommon;
using DataWranglerCommonWPF.Properties;
using ShotGridIntegration;

namespace DataWranglerCommonWPF.Login
{
	/// <summary>
	/// Interaction logic for LoginPage.xaml
	/// </summary>
	public partial class LoginPage : Page
	{
		private const bool AcceptInvalidCredentials = false;

		private Task<ShotGridLoginResponse>? m_runningLoginTask;
		public event Action OnSuccessfulLogin = delegate { };
		private bool m_allowLoginFromRefreshToken = true; //Only first time. Any time after first we should not have a valid refresh token.

		private ShotGridAPI? m_targetAPI;
		private ILoginCredentialProvider? m_credentialProvider;

		public LoginPage()
		{
			InitializeComponent();

			LoadingSpinnerInstance.Visibility = Visibility.Hidden;
			LoginErrorContainer.Visibility = Visibility.Hidden;

			//UserSettings settings = Properties.UserSettings.Default;
			//if (settings.ShouldRememberUserAndPass)
			//{
			//	Username.Text = settings.LastUserName;
			//	Password.Password = settings.LastUserPassword;
			//	RememberMeCheckbox.IsChecked = settings.ShouldRememberUserAndPass;
			//}

			LoginButton.Click += AttemptLogin;
			
		}

		public void Initialize(ShotGridAPI a_targetAPI, ILoginCredentialProvider a_credentialProvider)
		{
			m_targetAPI = a_targetAPI;
			m_credentialProvider = a_credentialProvider;

			if (m_allowLoginFromRefreshToken && !Keyboard.IsKeyDown(Key.LeftShift))
			{
				StartLoginAttempt();
				//m_runningLoginTask = a_targetAPI.TryLoginWithScriptKey(ShotGridApiKeyProvider.ShotGridApiScriptName, ShotGridApiKeyProvider.ShotGridApiScriptKey);
				//m_runningLoginTask.ContinueWith(a_task => OnLoginAttemptCompleted(a_task.Result));
				//OnLoginAttemptStarted();
				m_allowLoginFromRefreshToken = false;
			}
		}

		private void AttemptLogin(object a_sender, RoutedEventArgs a_e)
		{
			StartLoginAttempt();
		}

		private void StartLoginAttempt()
		{
			//UpdateSavedUserSettings();

			if (m_targetAPI == null)
			{
				throw new Exception();
			}

			if (m_runningLoginTask != null)
			{
				return;
			}

			LoginErrorContainer.Visibility = Visibility.Hidden;

			string apiKey;
			if (ShotGridApiKeyProvider.ShotGridApiScriptKey != null)
			{
				apiKey = ShotGridApiKeyProvider.ShotGridApiScriptKey;
			}
			else
			{
				apiKey = Password.Text;
			}

			m_runningLoginTask = m_targetAPI.TryLoginWithScriptKey(ShotGridApiKeyProvider.ShotGridApiScriptName, apiKey);
			m_runningLoginTask.ContinueWith(a_task => OnLoginAttemptCompleted(a_task.Result));
			OnLoginAttemptStarted();
		}

		private void OnLoginAttemptCompleted(ShotGridLoginResponse a_obj)
		{
			if (m_credentialProvider == null || m_targetAPI == null)
			{
				throw new Exception();
			}

			if (AcceptInvalidCredentials)
			{
				m_targetAPI.SetInvalidAuthentication();
			}

			if (a_obj.Success || AcceptInvalidCredentials)
			{
				m_credentialProvider.OAuthRefreshToken = m_targetAPI.GetCurrentCredentials().RefreshToken;
				OnSuccessfulLogin.Invoke();
			}
			else
			{
				Dispatcher.Invoke(() => {
					if (a_obj.ErrorResponse == null)
					{
						throw new Exception("Null error response");
					}

					LoginErrorMessage.Text = a_obj.ErrorResponse.Errors[0].Title;
					LoginErrorContainer.Visibility = Visibility.Visible;
				});
			}

			Dispatcher.Invoke(() => { 
				LoadingSpinnerInstance.Visibility = Visibility.Hidden; 
			});

			m_runningLoginTask = null;
		}

		private void OnLoginAttemptStarted()
		{
			LoadingSpinnerInstance.Visibility = Visibility.Visible;
		}
	}
}
