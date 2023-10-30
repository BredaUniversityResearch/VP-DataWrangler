using System.IO;
using System.Text;
using CommonLogging;
using DataWranglerCommon;
using DataWranglerCommon.CameraHandling;
using Newtonsoft.Json;

namespace DataWranglerInterface.Configuration
{
	public class DataWranglerInterfaceConfig
	{
		public static DataWranglerInterfaceConfig Instance { get; private set; } = null!;

		public List<ConfigActiveCameraGrouping> ConfiguredCameraGroupings = new List<ConfigActiveCameraGrouping>();
		public ConfigString ShotVersionNameTemplate = new ConfigString("Take_${ShotVersionId}");

		private bool m_needsToBeSaved = false;
		private readonly object m_saveSychPrimitive = new();

		public DataWranglerInterfaceConfig()
		{
			using (FileStream fs = new FileStream("Settings.json", FileMode.Open, FileAccess.Read))
			{
				using (StreamReader sr = new StreamReader(fs))
				{
					JsonConvert.PopulateObject(sr.ReadToEnd(), this);
				}
			}
		}

		public static void Use(DataWranglerInterfaceConfig a_instance)
		{
			if (Instance != null)
			{
				throw new Exception("Multiple assignments of singleton");
			}
			Instance = a_instance;
		}

		public void Save()
		{
			lock (m_saveSychPrimitive)
			{
				if (m_needsToBeSaved)
				{
					m_needsToBeSaved = false;
					using (FileStream fs = new FileStream("Settings.json", FileMode.Create, FileAccess.Write))
					{
						using (StreamWriter writer = new StreamWriter(fs, Encoding.UTF8))
						{
							writer.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
						}
					}
				}
			}
		}

		public void MarkDirty()
		{
			lock(m_saveSychPrimitive)
			{
				m_needsToBeSaved = true;
			}
		}
	}
}
