using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using ShotGridIntegration;

namespace DataWranglerInterface.Login
{
	/// <summary>
	/// Interaction logic for LoginPage.xaml
	/// </summary>
	public partial class LoginPage : Page
	{
		private Task<ShotGridLoginResponse>? m_runningLoginTask;
		public event Action OnSuccessfulLogin = delegate { };

		public LoginPage()
		{
			InitializeComponent();

			LoginErrorContainer.Visibility = Visibility.Hidden;

			LoginButton.Click += AttemptLogin;
			if (false && !string.IsNullOrEmpty(Properties.Settings.Default.OAuthRefreshToken))
			{
				m_runningLoginTask =
					DataWranglerServiceProvider.Instance.ShotGridAPI.TryLogin(Properties.Settings.Default
						.OAuthRefreshToken);
				m_runningLoginTask.ContinueWith(a_task => OnLoginAttemptCompleted(a_task.Result));
				OnLoginAttemptStarted();
			}
		}

		private void AttemptLogin(object a_sender, RoutedEventArgs a_e)
		{
			if (m_runningLoginTask != null)
			{
				return;
			}

			LoginErrorContainer.Visibility = Visibility.Hidden;

			string user = Username.Text;
			string password = Password.Password;

			m_runningLoginTask = DataWranglerServiceProvider.Instance.ShotGridAPI.TryLogin(user, password);
			m_runningLoginTask.ContinueWith(a_task => OnLoginAttemptCompleted(a_task.Result));
			OnLoginAttemptStarted();
		}

		private void OnLoginAttemptCompleted(ShotGridLoginResponse a_obj)
		{
			if (a_obj.Success == true)
			{
				Properties.Settings.Default.OAuthRefreshToken = DataWranglerServiceProvider.Instance.ShotGridAPI
					.GetCurrentCredentials().RefreshToken;
				Properties.Settings.Default.Save();
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

			m_runningLoginTask = null;
		}

		private void OnLoginAttemptStarted()
		{
		}

	}
}
