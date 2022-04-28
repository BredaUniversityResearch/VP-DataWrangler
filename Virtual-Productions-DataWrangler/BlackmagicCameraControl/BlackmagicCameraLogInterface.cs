using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackmagicCameraControl
{
	public abstract class BlackmagicCameraLogInterface
	{
		private static BlackmagicCameraLogInterface? ms_instance = null;

		public static void Use(BlackmagicCameraLogInterface a_instance)
		{
			ms_instance = a_instance;
		}

		public abstract void Log(string a_severity, string a_message);

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
	}
}
