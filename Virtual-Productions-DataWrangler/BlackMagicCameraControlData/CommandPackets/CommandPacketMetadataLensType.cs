namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(12, 9, 0, ECommandDataType.Utf8String)]
	public class CommandPacketMetadataLensType: ICommandPacketBase
	{
		public string Type; // [0-55]

		public CommandPacketMetadataLensType(CommandReader a_reader)
		{
			Type = a_reader.ReadString();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Type);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketMetadataLensType? other = (CommandPacketMetadataLensType?)a_other;
			return other != null &&
			       other.Type == Type;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Type}]";
		}
	}
}
