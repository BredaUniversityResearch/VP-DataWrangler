using BlackmagicCameraControlData;

namespace CameraControlOverEthernet.CameraControl;

public class CameraControlDataPacket : INetworkAPIPacket
{
    public string DeviceUuid;
    public uint ReceivedTimeCodeAsBCD;
    public byte[] PacketData;

    public CameraControlDataPacket(CameraDeviceHandle a_deviceHandle, TimeCode a_receivedTime, byte[] a_packetPayload)
    {
        DeviceUuid = a_deviceHandle.DeviceUuid;
        ReceivedTimeCodeAsBCD = a_receivedTime.TimeCodeAsBinaryCodedDecimal;
        PacketData = a_packetPayload;
    }
}