using AutoNotify;

namespace DataWranglerCommon;

public abstract partial class DataWranglerFileSourceMeta
{
	[AutoNotify]
	private string m_sourceType;

	[AutoNotify]
	private string m_sourceFileKind;

	public abstract bool IsUniqueMeta { get; } //Does it make sense if we have multiple of these meta entries on a single shot?

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
		throw new Exception("Unknown meta type received");
		//return new DataWranglerFileSourceMeta(m_sourceType, m_sourceFileKind);
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
		else if (a_sourceTypeName == DataWranglerFileSourceMetaTascam.MetaSourceType)
		{
			return new DataWranglerFileSourceMetaTascam();
		}

		throw new Exception($"Unknown source meta type {a_sourceTypeName}");
	}
}