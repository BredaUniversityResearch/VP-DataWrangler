using AutoNotify;
using CommonLogging;
using DataWranglerCommon.CameraHandling;
using DataWranglerCommon.ShogunLiveSupport;
using Newtonsoft.Json;
using ShotGridIntegration;

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
			if (m_targetService == null)
			{
				Logger.LogError("ViconIngestHandler", "Recording started with Vicon Tracking data, but ShogunLive service was null");
				return;
			}

			IngestDataSourceMetaViconTracking? trackingDataMeta = a_shotMetaData.FindMetaByType<IngestDataSourceMetaViconTracking>();
			if (trackingDataMeta != null)
			{
				if (m_targetService.StartCapture(trackingDataMeta.TempCaptureFileName, trackingDataMeta.TempCaptureLibraryPath, out var task))
				{
					task.ContinueWith((a_result) => {
						if (!a_result.Result)
						{
							Logger.LogError("ShotRecording", $"Vicon failed to send a confirmation that recording of library " +
							                                 $"{trackingDataMeta.TempCaptureLibraryPath} with file name {trackingDataMeta.TempCaptureFileName} started");
						}
					});
				}
				else
				{
					Logger.LogError("ShotRecording", "Failed to start shogun live recording.");
				}
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

		public override List<IngestFileEntry> ProcessCache(ShotGridEntityCache a_cache, IngestDataCache a_ingestCache)
		{
			List<IngestFileEntry> result = new List<IngestFileEntry>();

			var metasToParse = a_ingestCache.FindShotVersionsWithMeta<IngestDataSourceMetaViconTracking>();
			foreach (var currentMeta in metasToParse)
			{
				if (string.IsNullOrEmpty(currentMeta.Value.TempCaptureFileName) ||
					string.IsNullOrEmpty(currentMeta.Value.TempCaptureLibraryPath))
				{
					Logger.LogError("MetaFileResolverVicon", $"Shot meta specifies empty temp file name or empty capture library on shot {currentMeta.Key.Attributes.VersionCode}");
					continue;
				}

				int matchedCount = 0;
				if (Directory.Exists(currentMeta.Value.TempCaptureLibraryPath))
				{
					foreach (string filePaths in Directory.GetFiles(currentMeta.Value.TempCaptureLibraryPath))
					{
						FileInfo targetFile = new FileInfo(filePaths);
						if (targetFile.Name.StartsWith(currentMeta.Value.TempCaptureFileName))
						{
							result.Add(new IngestFileEntry(currentMeta.Key, targetFile.FullName, ViconTrackingFileTag));
							++matchedCount;
						}
					}
				}

				if (matchedCount == 0)
				{
					Logger.LogError("MetaFileResolverVicon", $"Failed to find any files to import for meta {currentMeta.Key.Attributes.VersionCode} " +
						$"at directory {currentMeta.Value.TempCaptureLibraryPath} with name {currentMeta.Value.TempCaptureFileName}");
				}
			}

			return result;
		}
	}
}
