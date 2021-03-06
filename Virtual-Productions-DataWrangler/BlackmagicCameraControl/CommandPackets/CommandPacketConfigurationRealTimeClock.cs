using System;
using System.Text;

namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(7, 0, 8, ECommandDataType.Int32)]
	public class CommandPacketConfigurationRealTimeClock: ICommandPacketBase
	{
		public int BinaryTimeCode = 0;
		public int BinaryDateCode = 0;

		public DateTime ClockTime;

		public CommandPacketConfigurationRealTimeClock(DateTime a_timePoint)
		{
			BinaryTimeCode = BinaryCodedDecimal.FromTime(a_timePoint);
			BinaryDateCode = BinaryCodedDecimal.FromDate(a_timePoint);
		}

		public CommandPacketConfigurationRealTimeClock(CommandReader a_reader)
		{
			BinaryTimeCode = a_reader.ReadInt32();
			BinaryDateCode = a_reader.ReadInt32();

			ClockTime = BinaryCodedDecimal.ToDateTime(BinaryDateCode, BinaryTimeCode);
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(BinaryTimeCode);
			a_writer.Write(BinaryDateCode);
		}

		public override string ToString()
		{
			return $"{GetType().Name} [0x{BinaryTimeCode:X}, 0x{BinaryDateCode:X}, Utility: {ClockTime}]";
		}
	}
}
