using AutoNotify;
using Newtonsoft.Json;

namespace DataWranglerCommon;

public abstract partial class DataWranglerFileSourceMeta
{
	[AutoNotify, JsonProperty("SourceType")]
	private string m_sourceType;

	[AutoNotify, JsonProperty("SourceFileTag")]
	private string m_sourceFileTag;

	public abstract bool IsUniqueMeta { get; } //Does it make sense if we have multiple of these meta entries on a single shot?

	protected DataWranglerFileSourceMeta()
	{
		m_sourceType = "Unknown";
		m_sourceFileTag = "";
	}

	protected DataWranglerFileSourceMeta(string a_sourceType, string a_sourceFileTag)
	{
		m_sourceType = a_sourceType;
		m_sourceFileTag = a_sourceFileTag;
	}

	public abstract DataWranglerFileSourceMeta Clone();

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
		else if (a_sourceTypeName == DataWranglerFileSourceMetaViconTrackingData.MetaSourceType)
		{
			return new DataWranglerFileSourceMetaViconTrackingData();
		}

		throw new Exception($"Unknown source meta type {a_sourceTypeName}");
	}

	public virtual void OnTemplateMetaCloned()
	{
	}

    public virtual void OnRecordingStarted(TimeCode a_stateChangeTime)
    {
    }

    public virtual void OnRecordingStopped()
    {
    }
}