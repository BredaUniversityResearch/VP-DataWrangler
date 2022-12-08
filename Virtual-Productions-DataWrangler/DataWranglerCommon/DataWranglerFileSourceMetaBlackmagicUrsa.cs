using AutoNotify;

namespace DataWranglerCommon;

public partial class DataWranglerFileSourceMetaBlackmagicUrsa: DataWranglerFileSourceMeta
{
	public static readonly string MetaSourceType = "BlackmagicUrsa";

	public static readonly TimeSpan MaxTimeOffset = new(0, 0, 5);

	public override bool IsUniqueMeta => true;

	[AutoNotify]
	private string m_source = "";

	[AutoNotify]
	private string m_codecName = "";

	[AutoNotify] 
	private DateTimeOffset? m_recordingStart = null;

	[AutoNotify] 
	private string m_storageTarget = "";

	public DataWranglerFileSourceMetaBlackmagicUrsa()
		: base(MetaSourceType, "video")
	{
	}

	public override DataWranglerFileSourceMetaBlackmagicUrsa Clone()
	{
		return new DataWranglerFileSourceMetaBlackmagicUrsa
		{
			m_source = m_source, 
			m_codecName = m_codecName, 
			m_recordingStart = m_recordingStart,
			m_storageTarget = m_storageTarget
		};
	}

	public bool IsSourceFor(DateTimeOffset a_fileInfoCreationTimeUtc, string a_storageName, string a_codecName, out string? a_reasonForRejection)
	{
		if (CodecName == a_codecName && StorageTarget == a_storageName)
		{
			TimeSpan? timeSinceCreation = a_fileInfoCreationTimeUtc - RecordingStart!;
			if (timeSinceCreation > -MaxTimeOffset && timeSinceCreation < MaxTimeOffset)
			{
				a_reasonForRejection = null;
				return true;
			}
			else
			{
				a_reasonForRejection = $"Time offset did not match. Offset was {timeSinceCreation.Value.TotalSeconds} seconds";
			}
		}
		else
		{
			a_reasonForRejection = $"Expected codec/storage {CodecName}/{StorageTarget}  got {a_codecName}/{a_storageName}";
		}
		return false;
	}
}
