namespace BlackmagicCameraControl
{
	public interface IBlackmagicCameraLogInterface
	{
		private static IBlackmagicCameraLogInterface? ms_instance = null;

		public static void Use(IBlackmagicCameraLogInterface a_instance)
		{
			ms_instance = a_instance;
		}

		public void Log(string a_severity, string a_message);

		public static void LogInfo(string a_message)
		{
			ms_instance?.Log("Info", a_message);
		}

		public static void LogWarning(string a_message)
		{
			ms_instance?.Log("Warning", a_message);
		}


		public static void LogError(string a_message)
		{
			ms_instance?.Log("Error", a_message);
		}

		public static void LogVerbose(string a_message)
		{
			ms_instance?.Log("Verbose", a_message);
		}
	}
}
