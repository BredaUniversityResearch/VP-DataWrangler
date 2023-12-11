using AutoNotify;

namespace DataApiCommon;

public partial class DataEntityProject: DataEntityBase
{
	[AutoNotify]
	private string m_name = "";

	[AutoNotify]
	private DataEntityReference? m_dataStore = null;
};