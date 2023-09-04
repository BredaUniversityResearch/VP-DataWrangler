namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(12, 7, 0, ECommandDataType.Utf8String)]
	public class CommandPacketMetadataDirector: ICommandPacketBase
	{
		public string Director; // [0-27]

		public CommandPacketMetadataDirector(CommandReader a_reader)
		{
			Director = a_reader.ReadString();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Director);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketMetadataDirector? other = (CommandPacketMetadataDirector?)a_other;
			return other != null &&
			       other.Director == Director;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Director}]";
		}
	}
}
