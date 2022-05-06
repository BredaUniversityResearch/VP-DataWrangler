using System;
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
		public event Action OnSuccessfulLogin = delegate { };

		public ShotGridAuthenticationWindow(ShotGridAPI a_api)
		{
			m_api = a_api;

			InitializeComponent();

			LoginContent.OnSuccessfulLogin += SuccessfulLoginCallback;
			LoginContent.Initialize(m_api, new SettingsCredentialProvider());
		}

		private void SuccessfulLoginCallback()
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.InvokeAsync(SuccessfulLoginCallback);
				return;
			}

			Close();

			OnSuccessfulLogin.Invoke();
		}

		public void EnsureLogin()
		{
			Show();
		}
	}
}
