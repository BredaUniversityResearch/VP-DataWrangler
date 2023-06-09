using AutoNotify;

namespace DataApiCommon
{
	public partial class DataEntityShotVersion: DataEntityBase
	{
		[AutoNotify]
		private string? m_dataWranglerMeta;

		[AutoNotify]
		private string m_shotVersionName = "";

		[AutoNotify]
		private string? m_description = "";

		[AutoNotify]
		private string? m_imageURL = null;

		[AutoNotify]
		private bool m_flagged = false;
	}
}
