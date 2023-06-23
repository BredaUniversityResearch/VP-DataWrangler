using System.Windows;
using System.Windows.Controls;

namespace DataWranglerCommonWPF.Login
{
	/// <summary>
	/// Interaction logic for APIConnectionPage.xaml
	/// </summary>
	public partial class APIConnectionPage : Page
	{
		public string VersionString => "V0.1";

		public APIConnectionPage()
		{
			InitializeComponent();

			ErrorMessageContainer.Visibility = Visibility.Hidden;
		}

		public void OnConnectFailure(string a_errorMessage)
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.InvokeAsync(() => OnConnectFailure(a_errorMessage));
				return;
			}

			ErrorMessageTextBlock.Text = a_errorMessage;
			ErrorMessageContainer.Visibility = Visibility.Visible;
			LoadingSpinnerInstance.Visibility = Visibility.Hidden;
		}
	}
}
