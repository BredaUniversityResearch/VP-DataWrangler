using CommonLogging;

namespace BlackmagicCameraControlData
{
	public static class BlackmagicCameraLogInterface
	{
		public static void LogVerbose(string a_message)
		{
			Logger.LogVerbose("BMAPI", a_message);
		}

		public static void LogInfo(string a_message)
		{
			Logger.LogInfo("BMAPI", a_message);
		}

		public static void LogWarning(string a_message)
		{
			Logger.LogWarning("BMAPI", a_message);
		}

		public static void LogError(string a_message)
		{
			Logger.LogError("BMAPI", a_message);
		}
	}
}
