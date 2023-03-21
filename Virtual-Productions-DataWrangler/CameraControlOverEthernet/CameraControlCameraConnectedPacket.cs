using BlackmagicCameraControlData;

namespace CameraControlOverEthernet;

public class CameraControlCameraConnectedPacket : ICameraControlPacket
{
	public string DeviceUuid;

	public CameraControlCameraConnectedPacket(CameraDeviceHandle a_deviceHandle)
	{
		DeviceUuid = a_deviceHandle.DeviceUuid;
	}
}