using System.Windows.Controls;
using BlackmagicCameraControl;
using DataWranglerInterface.DebugSupport;

namespace DataWranglerInterface
{
	/// <summary>
	/// Interaction logic for LogPage.xaml
	/// </summary>
	public partial class LogPage : Page, IBlackmagicCameraLogInterface
	{
		public LogPage()
		{
			InitializeComponent();

			Logger.Instance.OnMessageLogged += OnMessageLogged;

			IBlackmagicCameraLogInterface.Use(this);
			
		}

		private void OnMessageLogged(string a_source, string a_severity, string a_message)
		{
			Dispatcher.Invoke(() => { LogOutput.Text += $"{a_source}\t{a_severity}\t{a_message}\n"; });
		}

		public void Log(string a_severity, string a_message)
		{
			OnMessageLogged("LogBM", a_severity, a_message);
		}
	}
}
