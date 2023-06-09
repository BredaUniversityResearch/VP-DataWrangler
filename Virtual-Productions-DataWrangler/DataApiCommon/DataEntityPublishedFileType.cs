using AutoNotify;

namespace DataApiCommon
{
	public partial class DataEntityPublishedFileType: DataEntityBase
	{
		[AutoNotify]
		private string m_fileType = "";
	}
}
