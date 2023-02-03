namespace DataWranglerCommon.BRAWSupport;

public class BrawFileMetadata
{
	public readonly TimeCode FirstFrameTimeCode;
	public readonly string CameraNumber;
	public readonly DateTime DateRecorded; //YY MM DD with midnight

	public BrawFileMetadata(TimeCode a_firstFrameTimeCode, string a_cameraNumber, DateTime a_dateRecorded)
	{
		FirstFrameTimeCode = a_firstFrameTimeCode;
		CameraNumber = a_cameraNumber;
		DateRecorded = a_dateRecorded;
	}
}