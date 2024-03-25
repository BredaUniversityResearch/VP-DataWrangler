using BlackmagicCameraControlData;

namespace CameraControlOverEthernet.CameraControl;

public class CameraControlCameraDisconnectedPacket : INetworkAPIPacket
{
    public string DeviceUuid;

    public CameraControlCameraDisconnectedPacket(CameraDeviceHandle a_deviceHandle)
    {
        DeviceUuid = a_deviceHandle.DeviceUuid;
    }
}