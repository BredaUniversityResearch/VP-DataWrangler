using AutoNotify;
using Newtonsoft.Json;

namespace DataWranglerCommon.IngestDataSources
{
	public partial class IngestDataSourceMetaBlackmagicUrsa: IngestDataSourceMeta
	{
		[AutoNotify, JsonProperty("Source")]
		private string m_source = "";

		[AutoNotify, JsonProperty("CodecName")]
		private string m_codecName = "";

		[AutoNotify, JsonProperty("RecordingStart")]
		private DateTimeOffset m_recordingStart = DateTimeOffset.MinValue;

		[AutoNotify, JsonProperty("StartTimeCode")]
		private TimeCode m_startTimeCode = new();

		[AutoNotify, JsonProperty("CameraNumber")]
		private string m_cameraNumber = "A";

		public override IngestDataSourceMeta Clone()
		{
			return new IngestDataSourceMetaBlackmagicUrsa()
			{
				m_source = m_source,
				m_codecName = m_codecName,
				m_recordingStart = m_recordingStart,
				m_startTimeCode = m_startTimeCode,
				m_cameraNumber = m_cameraNumber
			};
		}
	}

	public class IngestDataSourceResolverBlackmagicUrsa : IngestDataSourceResolver
	{
		public override List<IngestFileEntry> ProcessDirectory(string a_baseDirectory, string a_storageName)
		{
			throw new NotImplementedException();
		}
	}
}
