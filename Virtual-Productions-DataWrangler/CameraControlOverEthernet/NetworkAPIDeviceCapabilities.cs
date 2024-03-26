namespace CameraControlOverEthernet
{
	public class NetworkAPIDeviceCapabilities
	{
		public enum EDeviceRole
		{
			Unknown = 0,
			CameraRelay = 1,
			ApplicationStateDisplay = 2,
		}

		public EDeviceRole DeviceRole = EDeviceRole.Unknown;
	}
}
