using System.IO;
using System.Text;
using CommonLogging;
using DataWranglerCommon;
using Newtonsoft.Json;

namespace DataWranglerInterface.Configuration
{
	public class DataWranglerConfig
	{
		public static DataWranglerConfig Instance { get; }

		public List<ConfigActiveCameraGrouping> ConfiguredCameraGroupings = new List<ConfigActiveCameraGrouping>();
		public ConfigString ShotVersionNameTemplate = new ConfigString("Take_${ShotVersionId}");

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

		public void Save()
		{
			using (FileStream fs = new FileStream("Settings.json", FileMode.Create, FileAccess.Write))
			{
				using (StreamWriter writer = new StreamWriter(fs, Encoding.UTF8))
				{
					writer.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
				}
			}
		}
	}

	public class ConfigActiveCameraGrouping
	{
		public string Name = "Virtual Camera";
		public List<string> DeviceHandleUuids = new List<string>();
	}
}
