using AutoNotify;

namespace DataApiCommon
{
	public partial class DataEntityShot: DataEntityBase
	{
		[AutoNotify]
		private string m_shotName = "";

		[AutoNotify]
		private string m_description = "";

		[AutoNotify]
		private string? m_imageURL;

		[AutoNotify]
		private string m_camera = "N/A";

		[AutoNotify]
		private string m_lens = "N/A";

		[AutoNotify]
		private IngestDataShotVersionMeta m_dataSourcesTemplate = new IngestDataShotVersionMeta();
	}
}
