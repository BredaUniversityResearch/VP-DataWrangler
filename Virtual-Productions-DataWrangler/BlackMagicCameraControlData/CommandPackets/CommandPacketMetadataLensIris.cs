namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(12, 10, 0, ECommandDataType.Utf8String)]
	public class CommandPacketMetadataLensIris : ICommandPacketBase
	{
		public string Iris; //[0-19]

		public CommandPacketMetadataLensIris(CommandReader a_reader)
		{
			Iris = a_reader.ReadString();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Iris);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketMetadataLensIris? other = (CommandPacketMetadataLensIris?)a_other;
			return other != null &&
			       other.Iris == Iris;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Iris}]";
		}
	}
}