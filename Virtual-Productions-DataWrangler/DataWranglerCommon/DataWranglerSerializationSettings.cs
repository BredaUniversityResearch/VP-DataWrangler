using DataWranglerCommon.IngestDataSources;
using Newtonsoft.Json;

namespace DataWranglerCommon
{
	public class DataWranglerSerializationSettings: JsonSerializerSettings
	{
		public static DataWranglerSerializationSettings Instance = new DataWranglerSerializationSettings();

		private DataWranglerSerializationSettings()
		{
			DateFormatHandling = DateFormatHandling.IsoDateFormat;
			Converters.Add(new IngestDataSourceMetaConverter());
			Converters.Add(new TimeCodeJsonConverter());
		}
	}
}
