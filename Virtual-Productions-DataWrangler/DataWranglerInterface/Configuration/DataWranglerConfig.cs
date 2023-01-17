using System.IO;
using CommonLogging;
using Newtonsoft.Json;

namespace DataWranglerInterface.Configuration
{
	public class DataWranglerConfig
	{
		public static DataWranglerConfig Instance { get; }

		public readonly List<string> StorageTargets = new List<string>();

		static DataWranglerConfig()
		{
			using (FileStream fs = new FileStream("Settings.json", FileMode.Open, FileAccess.Read))
			{
				using (StreamReader sr = new StreamReader(fs))
				{
					DataWranglerConfig? config = JsonConvert.DeserializeObject<DataWranglerConfig>(sr.ReadToEnd());
					if (config == null)
					{
						Logger.LogError("Settings", "Failed to load settings from file.");
						Instance = new DataWranglerConfig();
					}
					else
					{
						Instance = config;
					}
				}
			}
			
		}
	}
}
