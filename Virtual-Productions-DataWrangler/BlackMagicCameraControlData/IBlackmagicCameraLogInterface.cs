namespace BlackmagicCameraControl
{
	public interface IBlackmagicCameraLogInterface
	{
		public enum ELogSeverity
		{
			Verbose,
			Info,
			Warning,
			Error
		};

		private static IBlackmagicCameraLogInterface? ms_instance = null;

		public static void Use(IBlackmagicCameraLogInterface a_instance)
		{
			ms_instance = a_instance;
		}

		public void Log(string a_severity, string a_message);
		public ELogSeverity GetLogLevel();

		public static void LogWithSeverity(ELogSeverity a_severity, string a_message)
		{
			
			if (ms_instance != null && a_severity >= ms_instance.GetLogLevel())
			{
				ms_instance.Log(a_severity.ToString(), a_message);
			}
		}

		public static void LogVerbose(string a_message)
		{
			LogWithSeverity(ELogSeverity.Verbose, a_message);
		}

		public static void LogInfo(string a_message)
		{
			LogWithSeverity(ELogSeverity.Info, a_message);
		}

		public static void LogWarning(string a_message)
		{
			LogWithSeverity(ELogSeverity.Warning, a_message);
		}

		public static void LogError(string a_message)
		{
			LogWithSeverity(ELogSeverity.Error, a_message);
		}

	}
}
