﻿namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(9, 0, 6, ECommandDataType.Int16)]
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

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(BatteryVoltage_mV);
			a_writer.Write(BatteryPercentage);
			a_writer.Write(Unknown);
		}
	}
}
