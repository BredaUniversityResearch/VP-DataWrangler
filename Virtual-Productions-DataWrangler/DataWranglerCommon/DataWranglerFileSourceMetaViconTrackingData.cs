using AutoNotify;
using BlackmagicCameraControlData;
using Newtonsoft.Json;

namespace DataWranglerCommon
{
	public partial class DataWranglerFileSourceMetaViconTrackingData: DataWranglerFileSourceMeta
	{
		public static readonly string MetaSourceType = "Vicon Shogun Tracking Data";
		public override bool IsUniqueMeta => true;

		[AutoNotify, JsonProperty("TempDataBase")]
		private string m_tempCaptureLibraryPath = "C:/Temp/ViconTempDb/UNIT_TEST_DB";

		[AutoNotify, JsonProperty("TempFileName")] 
		private string m_tempCaptureFileName = "";

		public DataWranglerFileSourceMetaViconTrackingData()
			: base(MetaSourceType, "motion-data")
		{
		}

		public override DataWranglerFileSourceMeta Clone()
		{
			return new DataWranglerFileSourceMetaViconTrackingData()
			{
				m_tempCaptureLibraryPath = m_tempCaptureLibraryPath,
				m_tempCaptureFileName = m_tempCaptureFileName
			};
		}

        public override void OnRecordingStarted(TimeCode a_stateChangeTime)
        {
            base.OnRecordingStarted(a_stateChangeTime);

            m_tempCaptureFileName = "Capture_"+DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
	}
}
