namespace BlackmagicCameraControlData
{
	public class CameraHandleGenerator
	{
		private static int ms_lastUsedHandle = 1;

		public static CameraHandle Next()
		{
			return new CameraHandle(++ms_lastUsedHandle);
		}
	}
}
