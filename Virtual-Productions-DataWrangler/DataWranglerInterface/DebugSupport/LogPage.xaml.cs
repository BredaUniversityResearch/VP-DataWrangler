using System.Collections.ObjectModel;
using System.Windows.Controls;
using BlackmagicCameraControl;
using DataWranglerCommon;

namespace DataWranglerInterface.DebugSupport
{
	public class LogMessage
	{
		public string Source { get; }
		public string Severity { get; }
		public string Message { get; }

		public LogMessage(string a_source, string a_severity, string a_message)
		{
			Source = a_source;
			Severity = a_severity;
			Message = a_message;
		}
	};

	/// <summary>
	/// Interaction logic for LogPage.xaml
	/// </summary>
	public partial class LogPage : Page, IBlackmagicCameraLogInterface
	{
		public ObservableCollection<LogMessage> Messages { get; } = new ObservableCollection<LogMessage>();

		public LogPage()
		{
			InitializeComponent();

			Logger.Instance.OnMessageLogged += OnMessageLogged;

			IBlackmagicCameraLogInterface.Use(this);
			
		}

		private void OnMessageLogged(string a_source, string a_severity, string a_message)
		{
			Dispatcher.InvokeAsync(() => { Messages.Add(new LogMessage(a_source, a_severity, a_message)); });
		}

		public void Log(string a_severity, string a_message)
		{
			OnMessageLogged("BMAPI", a_severity, a_message);
		}

		public IBlackmagicCameraLogInterface.ELogSeverity GetLogLevel()
		{
			return IBlackmagicCameraLogInterface.ELogSeverity.Verbose;
		}
	}
}
