namespace CameraControlOverEthernet;

public class CameraControlRequestCurrentState : ICameraControlPacket
{
	public string DeviceUuid;

	public CameraControlRequestCurrentState(string a_targetDeviceUuid)
	{
		DeviceUuid = a_targetDeviceUuid;
	}
}