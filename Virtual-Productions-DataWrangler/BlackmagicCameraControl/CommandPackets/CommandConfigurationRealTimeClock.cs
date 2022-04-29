using System;

namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(7, 0, 8, ECommandDataType.Int32)]
	public class CommandConfigurationRealTimeClock: ICommandPacketBase
	{
		public int BinaryTimeCode = 0;
		public int BinaryDateCode = 0;

		public DateTime ClockTime;

		public CommandConfigurationRealTimeClock()
		{
		}

		public CommandConfigurationRealTimeClock(CommandReader a_reader)
		{
			BinaryTimeCode = a_reader.ReadInt32();
			BinaryDateCode = a_reader.ReadInt32();

			ClockTime = DateTime.FromBinary(((long) BinaryTimeCode << 32) | (long) BinaryDateCode);
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(BinaryTimeCode);
			a_writer.Write(BinaryDateCode);
		}
	}
}
