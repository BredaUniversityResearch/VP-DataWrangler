using AutoNotify;

namespace DataWranglerCommon
{
	public partial class DataWranglerFileSourceMetaViconTrackingData: DataWranglerFileSourceMeta
	{
		public static readonly string MetaSourceType = "Vicon Shogun Tracking Data";
		public override bool IsUniqueMeta => true;

		[AutoNotify]
		private string m_tempCaptureLibraryPath = "";

		[AutoNotify] private string m_tempCaptureFileName = "";

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

        public override void OnRecordingStopped()
        {
	        base.OnRecordingStopped();

	        throw new NotImplementedException("Should notify shogun to stop recording, and queue an import job. Just shogun might not be on the same machine so we cannot copy from here...");
        }
	}
}
