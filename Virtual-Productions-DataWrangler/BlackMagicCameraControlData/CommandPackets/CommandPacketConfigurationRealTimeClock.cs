using DataWranglerCommon;

namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(7, 0, 8, ECommandDataType.Int32)]
	public class CommandPacketConfigurationRealTimeClock: ICommandPacketBase
	{
		public uint BinaryTimeCode = 0;
		public uint BinaryDateCode = 0;

		public DateTime ClockTime;

		public CommandPacketConfigurationRealTimeClock(DateTime a_timePoint)
		{
			BinaryTimeCode = BinaryCodedDecimal.FromTime(a_timePoint);
			BinaryDateCode = BinaryCodedDecimal.FromDate(a_timePoint);
		}

		public CommandPacketConfigurationRealTimeClock(CommandReader a_reader)
		{
			BinaryTimeCode = a_reader.ReadUInt32();
			BinaryDateCode = a_reader.ReadUInt32();

			ClockTime = BinaryCodedDecimal.ToDateTime(BinaryDateCode, BinaryTimeCode);
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(BinaryTimeCode);
			a_writer.Write(BinaryDateCode);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketConfigurationRealTimeClock? other = (CommandPacketConfigurationRealTimeClock?) a_other;
			return other != null &&
			       other.BinaryTimeCode == BinaryTimeCode &&
			       other.BinaryDateCode == BinaryDateCode;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [0x{BinaryTimeCode:X}, 0x{BinaryDateCode:X}, Utility: {ClockTime}]";
		}
	}
}
