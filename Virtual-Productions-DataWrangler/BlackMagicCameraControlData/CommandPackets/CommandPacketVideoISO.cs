namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(1, 14, 4, ECommandDataType.Int32)]
	public class CommandPacketVideoISO: ICommandPacketBase
	{
		public int ISOValue = 0; //ISO value divided by 100, 1 == 100, 2 == 200, 4 == 400

		public CommandPacketVideoISO()
		{
		}

		public CommandPacketVideoISO(CommandReader a_reader)
		{
			ISOValue = a_reader.ReadInt32();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(ISOValue);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketVideoISO? other = (CommandPacketVideoISO?)a_other;
			return other != null &&
			       other.ISOValue == ISOValue;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{ISOValue}]";
		}
	}
}
