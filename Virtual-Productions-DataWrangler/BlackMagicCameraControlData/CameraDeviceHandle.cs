namespace BlackmagicCameraControlData
{
	public class CameraDeviceHandle
	{
		public readonly string DeviceUuid; //Device identifier that should be the same across different connection methods
		public readonly CameraControllerBase TargetController;

		public CameraDeviceHandle(string a_deviceUuid, CameraControllerBase a_targetController)
		{
			DeviceUuid = a_deviceUuid;
			TargetController = a_targetController;
		}

		public static bool operator ==(CameraDeviceHandle a_lhs, CameraDeviceHandle a_rhs)
		{
			return a_lhs.DeviceUuid == a_rhs.DeviceUuid;
		}

		public static bool operator !=(CameraDeviceHandle a_lhs, CameraDeviceHandle a_rhs)
		{
			return !(a_lhs == a_rhs);
		}

		public bool Equals(CameraDeviceHandle a_other)
		{
			return DeviceUuid == a_other.DeviceUuid;
		}

		public override bool Equals(object? a_obj)
		{
			return a_obj is CameraDeviceHandle other && Equals(other);
		}

		public override int GetHashCode()
		{
			return DeviceUuid.GetHashCode();
		}
	}
}
