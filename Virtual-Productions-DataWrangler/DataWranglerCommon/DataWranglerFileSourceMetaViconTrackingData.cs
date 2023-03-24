using AutoNotify;

namespace DataWranglerCommon
{
	public partial class DataWranglerFileSourceMetaViconTrackingData: DataWranglerFileSourceMeta
	{
		public static readonly string MetaSourceType = "Vicon Shogun Tracking Data";
		public override bool IsUniqueMeta => true;

		[AutoNotify]
		private string m_relativeCapturePath = "MotionData/";

		public DataWranglerFileSourceMetaViconTrackingData()
			: base(MetaSourceType, "motion-data")
		{
		}

		public override DataWranglerFileSourceMeta Clone()
		{
			return new DataWranglerFileSourceMetaViconTrackingData()
			{
				m_relativeCapturePath = m_relativeCapturePath
			};
		}
	}
}
