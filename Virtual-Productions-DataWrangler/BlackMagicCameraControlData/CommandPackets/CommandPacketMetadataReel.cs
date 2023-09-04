namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(12, 0, 2, ECommandDataType.Int16)]
	public class CommandPacketMetadataReel: ICommandPacketBase
	{
		public short Reel;	//0-999

		public CommandPacketMetadataReel()
		{
		}

		public CommandPacketMetadataReel(CommandReader a_reader)
		{
			Reel = a_reader.ReadInt16();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Reel);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketMetadataReel? other = (CommandPacketMetadataReel?)a_other;
			return other != null &&
			       other.Reel == Reel;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Reel}]";
		}
	}
}
