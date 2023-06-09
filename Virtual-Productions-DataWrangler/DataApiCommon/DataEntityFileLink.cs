using AutoNotify;

namespace DataApiCommon;

public partial class DataEntityFileLink: DataEntityBase
{
	[AutoNotify]
	private string m_fileName = "";

	[AutoNotify]
	private Uri? m_uriPath = null;

	public DataEntityFileLink()
	{
	}

	public DataEntityFileLink(Uri a_destinationPath)
	{
		m_fileName = Path.GetFileName(a_destinationPath.LocalPath);
		m_uriPath = a_destinationPath;
	}
}