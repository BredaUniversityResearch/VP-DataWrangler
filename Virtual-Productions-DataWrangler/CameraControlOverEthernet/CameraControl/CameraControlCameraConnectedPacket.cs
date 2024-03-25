using BlackmagicCameraControlData;

namespace CameraControlOverEthernet.CameraControl;

public class CameraControlCameraConnectedPacket : INetworkAPIPacket
{
    public string DeviceUuid;

    public CameraControlCameraConnectedPacket(CameraDeviceHandle a_deviceHandle)
    {
        DeviceUuid = a_deviceHandle.DeviceUuid;
    }
}