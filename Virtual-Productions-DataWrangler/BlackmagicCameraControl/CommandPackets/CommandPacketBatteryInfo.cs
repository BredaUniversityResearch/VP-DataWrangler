namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(9, 0, 6)]
	public class CommandPacketBatteryInfo: ICommandPacketBase
	{
		public short BatteryVoltage_mV;
		public short BatteryPercentage;
		public short Unknown;

		public CommandPacketBatteryInfo(CommandReader a_reader)
		{
			BatteryVoltage_mV = a_reader.ReadInt16();
			BatteryPercentage = a_reader.ReadInt16();
			Unknown = a_reader.ReadInt16();
		}
	}
}
