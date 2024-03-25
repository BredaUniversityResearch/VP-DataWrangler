namespace CameraControlOverEthernet.CameraControl;

public class CameraControlRequestCurrentState : INetworkAPIPacket
{
    public string DeviceUuid;

    public CameraControlRequestCurrentState(string a_targetDeviceUuid)
    {
        DeviceUuid = a_targetDeviceUuid;
    }
}