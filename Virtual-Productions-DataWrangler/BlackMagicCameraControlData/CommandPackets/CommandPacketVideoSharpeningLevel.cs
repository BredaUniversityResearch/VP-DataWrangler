namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(1, 8, 1, ECommandDataType.Int8)]
	public class CommandPacketVideoSharpeningLevel : ICommandPacketBase
	{
		public byte Level = 0; //0 - Off, 1 - Low, 2 - Medium, 3 - High;

		public CommandPacketVideoSharpeningLevel()
		{
		}

		public CommandPacketVideoSharpeningLevel(CommandReader a_reader)
		{
			Level = a_reader.ReadInt8();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Level);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketVideoSharpeningLevel? other = (CommandPacketVideoSharpeningLevel?)a_other;
			return other != null &&
			       other.Level == Level;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Level}]";
		}
	}
}
