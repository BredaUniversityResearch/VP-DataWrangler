
namespace DataWranglerInterface.DebugSupport
{
	public class Logger
	{
		public delegate void MessageLoggedDelegate(string source, string severity, string message);
		public event MessageLoggedDelegate OnMessageLogged = delegate { };

		private static Logger ms_instance = new Logger();
		public static Logger Instance => ms_instance;

		public static void Log(string a_source, string a_severity, string a_message)
		{
			ms_instance.OnMessageLogged.Invoke(a_source, a_severity, a_message);
		}

		public static void LogInfo(string a_source, string a_message)
		{
			Log(a_source, "Info", a_message);
		}

		public static void LogWarning(string a_source, string a_message)
		{
			Log(a_source, "Warning", a_message);
		}

		public static void LogError(string a_source, string a_message)
		{
			Log(a_source, "Error", a_message);
		}

	}
}
