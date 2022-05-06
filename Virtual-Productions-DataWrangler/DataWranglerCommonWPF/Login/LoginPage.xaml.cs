using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ShotGridIntegration;

namespace DataWranglerCommonWPF.Login
{
	/// <summary>
	/// Interaction logic for LoginPage.xaml
	/// </summary>
	public partial class LoginPage : Page
	{
		private Task<ShotGridLoginResponse>? m_runningLoginTask;
		public event Action OnSuccessfulLogin = delegate { };

		private ShotGridAPI? m_targetAPI;
		private ILoginCredentialProvider? m_credentialProvider;

		public LoginPage()
		{
			InitializeComponent();

			LoadingSpinnerInstance.Visibility = Visibility.Hidden;
			LoginErrorContainer.Visibility = Visibility.Hidden;

			LoginButton.Click += AttemptLogin;
			
		}

		public void Initialize(ShotGridAPI a_targetAPI, ILoginCredentialProvider a_credentialProvider)
		{
			m_targetAPI = a_targetAPI;
			m_credentialProvider = a_credentialProvider;

			if (!string.IsNullOrEmpty(a_credentialProvider.OAuthRefreshToken))
			{
				m_runningLoginTask = m_targetAPI.TryLogin(a_credentialProvider.OAuthRefreshToken);
				m_runningLoginTask.ContinueWith(a_task => OnLoginAttemptCompleted(a_task.Result));
				OnLoginAttemptStarted();
			}
		}

		private void AttemptLogin(object a_sender, RoutedEventArgs a_e)
		{
			if (m_targetAPI == null)
			{
				throw new Exception();
			}

			if (m_runningLoginTask != null)
			{
				return;
			}

			LoginErrorContainer.Visibility = Visibility.Hidden;

			string user = Username.Text;
			string password = Password.Password;

			m_runningLoginTask = m_targetAPI.TryLogin(user, password);
			m_runningLoginTask.ContinueWith(a_task => OnLoginAttemptCompleted(a_task.Result));
			OnLoginAttemptStarted();
		}

		private void OnLoginAttemptCompleted(ShotGridLoginResponse a_obj)
		{
			if (m_credentialProvider == null || m_targetAPI == null)
			{
				throw new Exception();
			}

			if (a_obj.Success)
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
