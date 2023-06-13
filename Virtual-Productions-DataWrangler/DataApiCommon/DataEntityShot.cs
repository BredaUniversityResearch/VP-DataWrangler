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
	}
}
