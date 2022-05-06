using System.Threading.Tasks;
using System.Windows;
using DataWranglerCommonWPF.Login;
using ShotGridIntegration;

namespace DataWranglerServiceWorker
{
	/// <summary>
	/// Interaction logic for ShotGridAuthenticationWindow.xaml
	/// </summary>
	public partial class ShotGridAuthenticationWindow : Window
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

		private ShotGridAPI m_api;

		public ShotGridAuthenticationWindow(ShotGridAPI a_api)
		{
			m_api = a_api;

			InitializeComponent();

			LoginContent.OnSuccessfulLogin += OnSuccessfulLogin;
			LoginContent.Initialize(m_api, new SettingsCredentialProvider());
		}

		private void OnSuccessfulLogin()
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.InvokeAsync(OnSuccessfulLogin);
				return;
			}

			Close();
		}

		public void EnsureLogin()
		{
			Show();
		}
	}
}
