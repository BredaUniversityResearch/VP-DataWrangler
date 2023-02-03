using AutoNotify;
using DataWranglerCommon.BRAWSupport;

namespace DataWranglerCommon;

public partial class DataWranglerFileSourceMetaBlackmagicUrsa: DataWranglerFileSourceMeta
{
	public static readonly string MetaSourceType = "BlackmagicUrsa";

	public static readonly TimeSpan MaxTimeOffset = new(0, 0, 5);
	public static readonly TimeSpan MaxTimeCodeOffset = new(0, 0, 2);

	public override bool IsUniqueMeta => true;

	[AutoNotify]
	private string m_source = "";

	[AutoNotify]
	private string m_codecName = "";

	[AutoNotify] 
	private DateTimeOffset m_recordingStart = DateTimeOffset.MinValue;

	[AutoNotify]
	private TimeCode m_startTimeCode = new();

	[AutoNotify, Obsolete("Now using CameraNumber & StartTimeCode to fix up files instead of RecordingStart & StorageTarget")] 
	private string m_storageTarget = "";

	[AutoNotify]
	private string m_cameraNumber = "-1";

	public DataWranglerFileSourceMetaBlackmagicUrsa()
		: base(MetaSourceType, "video")
	{
	}

	public override DataWranglerFileSourceMetaBlackmagicUrsa Clone()
	{
		return new DataWranglerFileSourceMetaBlackmagicUrsa
		{
			m_source = m_source,
			m_recordingStart = m_recordingStart,
			m_codecName = m_codecName, 
			m_startTimeCode = m_startTimeCode,
			m_cameraNumber =  m_cameraNumber,
		};
	}

	public bool IsSourceFor(FileInfo a_fileInfo, string a_storageName, string a_codecName, BrawFileMetadata? a_fileMeta, out string? a_reasonForRejection)
	{
		if (CodecName == a_codecName)
		{
			if (a_fileMeta != null &&
				StartTimeCode != TimeCode.Invalid && a_fileMeta.FirstFrameTimeCode != TimeCode.Invalid)
			{
				TimeSpan fileTimeCode = new TimeSpan(a_fileMeta.FirstFrameTimeCode.Hour, a_fileMeta.FirstFrameTimeCode.Minute, a_fileMeta.FirstFrameTimeCode.Second);
				TimeSpan metaTimeCode = new TimeSpan(StartTimeCode.Hour, StartTimeCode.Minute, StartTimeCode.Second);
				TimeSpan timeCodeDiff = fileTimeCode - metaTimeCode;
				if (timeCodeDiff > -MaxTimeCodeOffset && timeCodeDiff < MaxTimeCodeOffset)
				{
					if (a_fileMeta.DateRecorded != RecordingStart.Date)
					{
						a_reasonForRejection = $"Recording date: Meta: {RecordingStart.Date} File: {a_fileMeta.DateRecorded.Date}";
						return false;
					}

					if (a_fileMeta.CameraNumber == CameraNumber)
					{
						a_reasonForRejection = null;
						return true;
					}
					else
					{
						a_reasonForRejection = $"Wrong camera number: Meta: {CameraNumber} File: {a_fileMeta.CameraNumber}";
					}
				}
				else
				{
					a_reasonForRejection = $"First frame time code mismatch: Meta: {StartTimeCode} File: {a_fileMeta.FirstFrameTimeCode}";
				}
			}
			else
			{
				if (StorageTarget == a_storageName)
				{
					TimeSpan timeSinceCreation = a_fileInfo.CreationTimeUtc - RecordingStart;
					if (timeSinceCreation > -MaxTimeOffset && timeSinceCreation < MaxTimeOffset)
					{
						a_reasonForRejection = null;
						return true;
					}
					else
					{
						a_reasonForRejection =
							$"TimeCode invalid (Meta: {StartTimeCode} File: {a_fileMeta?.FirstFrameTimeCode}). File creation time offset did not match. Offset was {timeSinceCreation.TotalSeconds} seconds";
					}
				}
				else
				{
					a_reasonForRejection = $"Expected codec/storage {CodecName}/{StorageTarget} got {a_codecName}/{a_storageName}";
				}
			}
		}
		else
		{
			a_reasonForRejection = $"Expected codec/storage {CodecName}/{StorageTarget} got {a_codecName}/{a_storageName}";
		}
		return false;
	}
}
