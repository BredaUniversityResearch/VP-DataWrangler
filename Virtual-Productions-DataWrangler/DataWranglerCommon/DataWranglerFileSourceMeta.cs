using AutoNotify;

namespace DataWranglerCommon;

public partial class DataWranglerFileSourceMeta
{
	[AutoNotify]
	private string m_sourceType;

	[AutoNotify]
	private string m_sourceFileKind;

	public DataWranglerFileSourceMeta()
	{
		m_sourceType = "Unknown";
		m_sourceFileKind = "";
	}

	public DataWranglerFileSourceMeta(string a_sourceType, string a_sourceFileKind)
	{
		m_sourceType = a_sourceType;
		m_sourceFileKind = a_sourceFileKind;
	}

	public virtual DataWranglerFileSourceMeta Clone()
	{
		return new DataWranglerFileSourceMeta(m_sourceType, m_sourceFileKind);
	}

	public virtual bool IsSourceFor(DateTimeOffset a_fileInfoCreationTimeUtc, string a_storageName, string a_codecName)
	{
		return false;
	}

	public static DataWranglerFileSourceMeta CreateFromTypeName(string a_sourceTypeName)
	{
		if (a_sourceTypeName == DataWranglerFileSourceMetaBlackmagicUrsa.MetaSourceType)
		{
			return new DataWranglerFileSourceMetaBlackmagicUrsa();
		}

		throw new Exception($"Unknown source meta type {a_sourceTypeName}");
		return new DataWranglerFileSourceMeta();
	}
}