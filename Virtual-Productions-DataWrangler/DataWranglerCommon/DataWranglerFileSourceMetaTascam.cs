using AutoNotify;

namespace DataWranglerCommon;

public partial class DataWranglerFileSourceMetaTascam: DataWranglerFileSourceMeta
{
	public static readonly string MetaSourceType = "Tascam DR-60D MkII";

	public override bool IsUniqueMeta => true;

	[AutoNotify]
	private string m_filePrefix = "TASCAM_";

	[AutoNotify]
	private int m_fileIndex = 0;

	public DataWranglerFileSourceMetaTascam()
		: base(MetaSourceType, "audio")
	{
	}

	public override DataWranglerFileSourceMetaTascam Clone()
	{
		return new DataWranglerFileSourceMetaTascam
		{
			m_fileIndex = m_fileIndex, 
		};
	}

	public override bool IsSourceFor(DateTimeOffset a_fileInfoCreationTimeUtc, string a_storageName, string a_codecName)
	{
		throw new NotImplementedException();
	}
}