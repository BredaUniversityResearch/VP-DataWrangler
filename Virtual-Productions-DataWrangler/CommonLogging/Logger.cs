
namespace CommonLogging
{
	public class Logger
	{
		public delegate void MessageLoggedDelegate(string source, ELogSeverity severity, string message);
		public event MessageLoggedDelegate OnMessageLogged = delegate { };

		private static Logger ms_instance = new Logger();
		public static Logger Instance => ms_instance;

		private readonly List<LogMessage> m_messageLogHistory = new List<LogMessage>(64);
		public IReadOnlyList<LogMessage> LogHistory => m_messageLogHistory;

		public static void Log(string a_source, ELogSeverity a_severity, string a_message)
		{
			ms_instance.m_messageLogHistory.Add(new LogMessage(a_source, a_severity, a_message));
			ms_instance.OnMessageLogged.Invoke(a_source, a_severity, a_message);
		}

		public static void LogVerbose(string a_source, string a_message)
		{
			Log(a_source, ELogSeverity.Verbose, a_message);
		}

		public static void LogInfo(string a_source, string a_message)
		{
			Log(a_source, ELogSeverity.Info, a_message);
		}

		public static void LogWarning(string a_source, string a_message)
		{
			Log(a_source, ELogSeverity.Warning, a_message);
		}

		public static void LogError(string a_source, string a_message)
		{
			Log(a_source, ELogSeverity.Error, a_message);
		}
	}
}
