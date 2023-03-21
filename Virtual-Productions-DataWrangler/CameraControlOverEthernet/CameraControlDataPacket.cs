using BlackmagicCameraControlData;
using DataWranglerCommon;

namespace CameraControlOverEthernet;

public class CameraControlDataPacket: ICameraControlPacket
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