using AutoNotify;
using CommonLogging;
using DataApiCommon;
using DataWranglerCommon.CameraHandling;
using DataWranglerCommon.ShogunLiveSupport;
using Newtonsoft.Json;

namespace DataWranglerCommon.IngestDataSources
{
	[IngestDataSourceMeta(typeof(IngestDataSourceHandlerViconTracking), typeof(IngestDataSourceResolverViconTracking))]
	public partial class IngestDataSourceMetaViconTracking: IngestDataSourceMeta
	{
		public override string SourceType => "Vicon Shogun Tracking Data";

		[AutoNotify, JsonProperty("TempDataBase"), IngestDataEditable(EDataEditFlags.Visible, EDataEditFlags.Editable)]
		private string m_tempCaptureLibraryPath = "C:/Temp/ViconTempDb/UNIT_TEST_DB";

		[AutoNotify, JsonProperty("TempFileName"), IngestDataEditable(EDataEditFlags.Visible, EDataEditFlags.None)]
		private string m_tempCaptureFileName = "";

		public override IngestDataSourceMeta Clone()
		{
			return new IngestDataSourceMetaViconTracking()
			{
				m_tempCaptureFileName = m_tempCaptureFileName,
				m_tempCaptureLibraryPath = m_tempCaptureLibraryPath
			};
		}
	}

	public class IngestDataSourceHandlerViconTracking : IngestDataSourceHandler
	{
		private ShogunLiveService? m_targetService = null;

		public override void InstallHooks(DataWranglerEventDelegates a_eventDelegates, DataWranglerServices a_services)
		{
			a_eventDelegates.OnRecordingStarted += OnRecordingStarted;
			a_eventDelegates.OnRecordingFinished += OnRecordingFinished;
			m_targetService = a_services.ShogunLiveService;
		}

		private void OnRecordingStarted(ActiveCameraInfo a_sourceCamera, IngestDataShotVersionMeta a_shotMetaData)
		{
			IngestDataSourceMetaViconTracking? trackingDataMeta = a_shotMetaData.FindMetaByType<IngestDataSourceMetaViconTracking>();
			if (trackingDataMeta != null)
			{
				//Generate a new 'random' file name 
				trackingDataMeta.TempCaptureFileName = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

				if (m_targetService == null)
				{
					Logger.LogError("ViconIngestHandler", "Recording started with Vicon Tracking data, but ShogunLive service was null");
					return;
				}

				m_targetService.AsyncStartCapture(trackingDataMeta.TempCaptureFileName, trackingDataMeta.TempCaptureLibraryPath);
			}
		}


		private void OnRecordingFinished(ActiveCameraInfo a_sourceCamera, IngestDataShotVersionMeta a_shotMetaData)
		{
			if (m_targetService == null)
			{
				Logger.LogError("ViconIngestHandler", "Recording finished with Vicon Tracking data, but ShogunLive service was null");
				return;
			}

			IngestDataSourceMetaViconTracking? trackingDataMeta = a_shotMetaData.FindMetaByType<IngestDataSourceMetaViconTracking>();
			if (trackingDataMeta != null)
			{
				if (!m_targetService.StopCapture(true, trackingDataMeta.TempCaptureFileName, trackingDataMeta.TempCaptureLibraryPath))
				{
					Logger.LogError("ShotRecording", $"Failed to stop shogun capture of file {trackingDataMeta.TempCaptureFileName} in library {trackingDataMeta.TempCaptureLibraryPath}");
				}
			}
		}
	}

	public class IngestDataSourceResolverViconTracking : IngestDataSourceResolver
	{
		private const string ViconTrackingFileTag = "motion-data";

		public IngestDataSourceResolverViconTracking()
			: base(false, true)
		{
		}

		public override List<IngestFileResolutionDetails> ProcessCache(DataEntityCache a_cache, IngestDataCache a_ingestCache)
		{
			List<IngestFileResolutionDetails> result = new List<IngestFileResolutionDetails>();

			var metasToParse = a_ingestCache.FindShotVersionsWithMeta<IngestDataSourceMetaViconTracking>();
			foreach (var currentMeta in metasToParse)
			{
				if (string.IsNullOrEmpty(currentMeta.Value.TempCaptureFileName) ||
					string.IsNullOrEmpty(currentMeta.Value.TempCaptureLibraryPath))
				{
					Logger.LogError("MetaFileResolverVicon", $"Shot meta specifies empty temp file name or empty capture library on shot {currentMeta.Key.ShotVersionName}");
					continue;
				}

				int matchedCount = 0;
				if (Directory.Exists(currentMeta.Value.TempCaptureLibraryPath))
				{
					foreach (string filePaths in Directory.GetFiles(currentMeta.Value.TempCaptureLibraryPath))
					{
						IngestFileResolutionDetails fileDetails = new IngestFileResolutionDetails(filePaths);


						FileInfo targetFile = new FileInfo(filePaths);
						if (targetFile.Name.StartsWith(currentMeta.Value.TempCaptureFileName))
						{
							fileDetails.SetSuccessfulResolution(currentMeta.Key, ViconTrackingFileTag);
							result.Add(fileDetails);
							++matchedCount;
						}
					}
				}

				if (matchedCount == 0)
				{
					Logger.LogError("MetaFileResolverVicon", $"Failed to find any files to import for meta {currentMeta.Key.ShotVersionName} " +
						$"at directory {currentMeta.Value.TempCaptureLibraryPath} with name {currentMeta.Value.TempCaptureFileName}");
				}
			}

			return result;
		}
	}
}
