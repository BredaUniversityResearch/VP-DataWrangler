using BlackmagicCameraControlData;
using BlackmagicCameraControlData.CommandPackets;
using DataWranglerCommon;

namespace CameraControlOverEthernet;

public class CameraControlDataPacket: ICameraControlPacket
{
	public string DeviceUuid;
	public uint ReceivedTimeCodeAsBCD;
	public byte[] PacketData;

	public CameraControlDataPacket(CameraDeviceHandle a_deviceHandle, TimeCode a_receivedTime, ICommandPacketBase a_packet)
	{
		DeviceUuid = a_deviceHandle.DeviceUuid;
		ReceivedTimeCodeAsBCD = a_receivedTime.TimeCodeAsBinaryCodedDecimal;
		using (MemoryStream ms = new MemoryStream(128))
		{
			CommandWriter writer = new CommandWriter(ms);
			a_packet.WriteTo(writer);
			PacketData = ms.ToArray();
		}
	}
}