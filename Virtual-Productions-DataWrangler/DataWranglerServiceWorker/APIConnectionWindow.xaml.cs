using System;
using System.Windows;
using CommonLogging;
using DataApiCommon;

namespace DataWranglerServiceWorker
{
	/// <summary>
	/// Interaction logic for ShotGridAuthenticationWindow.xaml
	/// </summary>
	public partial class APIConnectionWindow : Window
	{
		public event Action OnSuccessfulConnect = delegate { };

		public APIConnectionWindow(DataApi a_api)
		{
			InitializeComponent();

			a_api.StartConnect().ContinueWith((a_taskResult) => {
				if (a_taskResult.Result)
				{
					SuccessfulLoginCallback();
				}
				else
				{
					Logger.LogError("API", "Failed to connect to API");
				}
			});
		}

		private void SuccessfulLoginCallback()
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.InvokeAsync(SuccessfulLoginCallback);
				return;
			}

			Close();

			OnSuccessfulConnect.Invoke();
		}
	}
}
