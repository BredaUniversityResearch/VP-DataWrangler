using System.Collections.ObjectModel;
using System.Windows.Controls;
using CommonLogging;

namespace DataWranglerInterface.DebugSupport
{
	/// <summary>
	/// Interaction logic for LogPage.xaml
	/// </summary>
	public partial class LogPage : Page
	{
		public ObservableCollection<LogMessage> Messages { get; }

		public LogPage()
		{
			Messages = new ObservableCollection<LogMessage>(Logger.Instance.LogHistory);

			InitializeComponent();

			Logger.Instance.OnMessageLogged += OnMessageLogged;
		}

		private void OnMessageLogged(TimeOnly a_time, string a_source, ELogSeverity a_severity, string a_message)
		{
			Dispatcher.InvokeAsync(() => { Messages.Add(new LogMessage(a_time, a_source, a_severity, a_message)); });
		}

		public ELogSeverity GetLogLevel()
		{
			return ELogSeverity.Verbose;
		}
	}
}
