namespace DataWranglerCommon.BRAWSupport;

public class BRAWFileMetadata
{
	public readonly TimeCode FirstFrameTimeCode;
	public readonly string CameraNumber;
	public readonly DateTime DateRecorded; //YY MM DD with midnight

	public BRAWFileMetadata(TimeCode a_firstFrameTimeCode, string a_cameraNumber, DateTime a_dateRecorded)
	{
		FirstFrameTimeCode = a_firstFrameTimeCode;
		CameraNumber = a_cameraNumber;
		DateRecorded = a_dateRecorded;
	}
}