using BlackmagicCameraControlData;

namespace CameraControlOverEthernet;

public class CameraControlCameraDisconnectedPacket : ICameraControlPacket
{
	public string DeviceUuid;

	public CameraControlCameraDisconnectedPacket(CameraDeviceHandle a_deviceHandle)
	{
		DeviceUuid = a_deviceHandle.DeviceUuid;
	}
}