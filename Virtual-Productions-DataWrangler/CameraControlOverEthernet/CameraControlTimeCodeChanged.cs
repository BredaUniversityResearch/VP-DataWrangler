using BlackmagicCameraControlData;

namespace CameraControlOverEthernet
{
	public class CameraControlTimeCodeChanged: ICameraControlPacket
	{
		public string DeviceUuid;
		public uint TimeCodeAsBCD;

		public CameraControlTimeCodeChanged(CameraDeviceHandle a_deviceUuid, TimeCode a_timeCode)
		{
			DeviceUuid = a_deviceUuid.DeviceUuid;
			TimeCodeAsBCD = a_timeCode.TimeCodeAsBinaryCodedDecimal;
		}
	}
}
