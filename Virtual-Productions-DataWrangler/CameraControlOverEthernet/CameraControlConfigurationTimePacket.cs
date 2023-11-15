namespace CameraControlOverEthernet
{
	internal class CameraControlConfigurationTimePacket : ICameraControlPacket
	{
		public int TimeZoneOffsetMinutes = 0;
		public uint BinaryTimeCode = 0; //BCD
		public uint BinaryDateCode = 0;

		public CameraControlConfigurationTimePacket(short a_timeZoneOffsetMinutes, uint a_timeCodeAsBCD, uint a_dateAsBCD)
		{
			TimeZoneOffsetMinutes = a_timeZoneOffsetMinutes;
			BinaryTimeCode = a_timeCodeAsBCD;
			BinaryDateCode = a_dateAsBCD;
		}
	}
}
