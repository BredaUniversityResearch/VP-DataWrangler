using System.Text.RegularExpressions;
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

	public bool IsSourceFor(FileInfo a_fileInfo, string a_storageName)
	{
		//TASCAM_0040S12.wav, TASCAM_0040S34D06.wav
		Regex filePattern = new Regex($"{m_filePrefix}([0-9]{{4}})(.*).wav");
		Match fileNameMatch = filePattern.Match(a_fileInfo.Name);
		if (fileNameMatch.Success)
		{
			if (int.TryParse(fileNameMatch.Groups[1].Value, out int targetFileIndex))
			{
				if (m_fileIndex == targetFileIndex)
				{
					return true;
				}
			}
		}

		return false;
	}

	public override void OnTemplateMetaCloned()
	{
		base.OnTemplateMetaCloned();
		++FileIndex;
	}
}