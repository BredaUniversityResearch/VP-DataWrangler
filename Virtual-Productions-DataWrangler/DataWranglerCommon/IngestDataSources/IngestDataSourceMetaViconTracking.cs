using AutoNotify;
using CommonLogging;
using DataWranglerCommon.CameraHandling;
using DataWranglerCommon.ShogunLiveSupport;
using Newtonsoft.Json;

namespace DataWranglerCommon.IngestDataSources
{
	public partial class IngestDataSourceMetaViconTracking: IngestDataSourceMeta
	{
		public override string SourceType => "Vicon Shogun Tracking Data";

		[AutoNotify, JsonProperty("TempDataBase")]
		private string m_tempCaptureLibraryPath = "C:/Temp/ViconTempDb/UNIT_TEST_DB";

		[AutoNotify, JsonProperty("TempFileName")]
		private string m_tempCaptureFileName = "";

		public override IngestDataSourceMeta Clone()
		{
			throw new NotImplementedException();
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
}
