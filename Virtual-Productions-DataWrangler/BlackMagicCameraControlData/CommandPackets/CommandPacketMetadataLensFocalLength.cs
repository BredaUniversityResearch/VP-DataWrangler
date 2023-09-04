namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(12, 11, 0, ECommandDataType.Utf8String)]
	public class CommandPacketMetadataLensFocalLength: ICommandPacketBase
	{
		public string FocalLength;	//

		public CommandPacketMetadataLensFocalLength(CommandReader a_reader)
		{
			FocalLength = a_reader.ReadString();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(FocalLength);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketMetadataLensFocalLength? other = (CommandPacketMetadataLensFocalLength?)a_other;
			return other != null &&
			       other.FocalLength == FocalLength;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{FocalLength}]";
		}
	}
}
