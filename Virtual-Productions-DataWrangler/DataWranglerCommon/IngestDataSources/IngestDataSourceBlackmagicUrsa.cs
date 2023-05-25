using AutoNotify;
using BlackmagicCameraControlData;
using DataWranglerCommon.BRAWSupport;
using DataWranglerCommon.CameraHandling;
using Newtonsoft.Json;
using ShotGridIntegration;

namespace DataWranglerCommon.IngestDataSources
{
	[IngestDataSourceMeta(typeof(IngestDataSourceHandlerBlackmagicUrsa), typeof(IngestDataSourceResolverBlackmagicUrsa))]
    public partial class IngestDataSourceMetaBlackmagicUrsa: IngestDataSourceMeta
	{
		[AutoNotify, JsonProperty("CodecName")]
		private string m_codecName = "";

		[AutoNotify, JsonProperty("RecordingStart")]
		private DateTimeOffset m_recordingStart = DateTimeOffset.MinValue;

		[AutoNotify, JsonProperty("StartTimeCode")]
		private TimeCode m_startTimeCode = new();

		[AutoNotify, JsonProperty("CameraNumber")]
		private string m_cameraNumber = "A";

		public override string SourceType => "BlackmagicUrsa";

		public override IngestDataSourceMeta Clone()
		{
			return new IngestDataSourceMetaBlackmagicUrsa()
			{
				m_codecName = m_codecName,
				m_recordingStart = m_recordingStart,
				m_startTimeCode = m_startTimeCode,
				m_cameraNumber = m_cameraNumber
			};
		}
	}

	public class IngestDataSourceResolverBlackmagicUrsa : IngestDataSourceResolver
	{
		private const string ImportedFileTag = "video";
		private static readonly TimeSpan MaxTimeCodeOffset = new(0, 0, 2);

		public override List<IngestFileEntry> ProcessDirectory(string a_baseDirectory, string a_storageVolumeName, ShotGridEntityCache a_cache, IngestDataCache a_ingestCache)
		{
			List<IngestFileEntry> result = new();

			var relevantMeta = a_ingestCache.FindShotVersionsWithMeta<IngestDataSourceMetaBlackmagicUrsa>();

			List<KeyValuePair<ShotGridEntityShotVersion, string>> rejectionLog = new();

			using BRAWFileDecoder fileDecoder = new BRAWFileDecoder();

			foreach (string filePath in Directory.EnumerateFiles(a_baseDirectory))
			{
				FileInfo fileInfo = new FileInfo(filePath);
				if (BlackmagicCameraCodec.FindFromFileExtension(fileInfo.Extension, out EBlackmagicCameraCodec codec))
				{
					BRAWFileMetadata? fileMeta = null;
					if (codec == EBlackmagicCameraCodec.BlackmagicRAW)
					{
						fileMeta = fileDecoder.GetMetaForFile(fileInfo);
					}

					if (fileMeta != null)
					{
						foreach (var targetShotMeta in relevantMeta)
						{
							if (targetShotMeta.Value.CodecName != codec.ToString())
							{
								rejectionLog.Add(new(targetShotMeta.Key, $"Wrong codec: Meta: {targetShotMeta.Value.CodecName} File: {codec}"));
								continue;
							}
							else if (targetShotMeta.Value.StartTimeCode == TimeCode.Invalid)
							{
								rejectionLog.Add(new(targetShotMeta.Key, $"Meta is contains invalid start time code: Meta: {targetShotMeta.Value.StartTimeCode}"));
								continue;
							}
							else if (fileMeta.FirstFrameTimeCode == TimeCode.Invalid)
							{
								//Reject on file meta not being correct.
								rejectionLog.Add(new(targetShotMeta.Key, $"File contains invalid first frame time code: File: {fileMeta.FirstFrameTimeCode}"));
								continue;
							}

							TimeCode startTimeCode = targetShotMeta.Value.StartTimeCode;
							TimeSpan fileTimeCode = new TimeSpan(fileMeta.FirstFrameTimeCode.Hour, fileMeta.FirstFrameTimeCode.Minute, fileMeta.FirstFrameTimeCode.Second);
							TimeSpan metaTimeCode = new TimeSpan(startTimeCode.Hour, startTimeCode.Minute, startTimeCode.Second);
							TimeSpan timeCodeDiff = fileTimeCode - metaTimeCode;
							if (timeCodeDiff > -MaxTimeCodeOffset && timeCodeDiff < MaxTimeCodeOffset)
							{
								if (fileMeta.DateRecorded != targetShotMeta.Value.RecordingStart.Date)
								{
									rejectionLog.Add(new(targetShotMeta.Key, $"Recording date: Meta: {targetShotMeta.Value.RecordingStart.Date} File: {fileMeta.DateRecorded.Date}"));
									continue;
								}
								else if (fileMeta.CameraNumber != targetShotMeta.Value.CameraNumber)
								{
									rejectionLog.Add(new(targetShotMeta.Key, $"Wrong camera number: Meta: {targetShotMeta.Value.CameraNumber} File: {fileMeta.CameraNumber}"));
									continue;
								}

								result.Add(new IngestFileEntry(targetShotMeta.Key, filePath, ImportedFileTag));
							}
						}
					}
				}
			}

			return result;
		}	
	}

	public class IngestDataSourceHandlerBlackmagicUrsa : IngestDataSourceHandler
	{
		public override void InstallHooks(DataWranglerEventDelegates a_eventDelegates, DataWranglerServices a_services)
		{
			a_eventDelegates.OnRecordingStarted += OnRecordingStart;
		}

		private void OnRecordingStart(ActiveCameraInfo a_sourceCamera, IngestDataShotVersionMeta a_shotMetaData)
		{
			IngestDataSourceMetaBlackmagicUrsa? dataSource = a_shotMetaData.FindMetaByType<IngestDataSourceMetaBlackmagicUrsa>();
			if (dataSource != null)
			{
				dataSource.StartTimeCode = a_sourceCamera.CurrentTimeCode;
				dataSource.CodecName = a_sourceCamera.SelectedCodec;
				dataSource.RecordingStart = DateTimeOffset.UtcNow;
				dataSource.CameraNumber = a_sourceCamera.CameraNumber;
			}
		}
	};
}
