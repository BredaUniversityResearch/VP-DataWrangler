namespace DataWranglerCommon.BRAWSupport;

public class BrawFileMetadata
{
	public readonly TimeCode FirstFrameTimeCode;
	public readonly string CameraNumber;

	public BrawFileMetadata(TimeCode a_firstFrameTimeCode, string a_cameraNumber)
	{
		FirstFrameTimeCode = a_firstFrameTimeCode;
		CameraNumber = a_cameraNumber;
	}
}