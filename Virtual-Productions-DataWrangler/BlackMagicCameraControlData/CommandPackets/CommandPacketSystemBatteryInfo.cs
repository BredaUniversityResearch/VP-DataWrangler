namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(9, 0, 6, ECommandDataType.Int16)]
	public class CommandPacketSystemBatteryInfo : ICommandPacketBase
	{
		public short BatteryVoltage_mV;
		public short BatteryPercentage;
		public short Unknown;

		public CommandPacketSystemBatteryInfo()
		{
		}

		public CommandPacketSystemBatteryInfo(CommandReader a_reader)
		{
			BatteryVoltage_mV = a_reader.ReadInt16();
			BatteryPercentage = a_reader.ReadInt16();
			Unknown = a_reader.ReadInt16();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(BatteryVoltage_mV);
			a_writer.Write(BatteryPercentage);
			a_writer.Write(Unknown);
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{BatteryVoltage_mV}, {BatteryPercentage}, {Unknown}]";

		}
	}
}