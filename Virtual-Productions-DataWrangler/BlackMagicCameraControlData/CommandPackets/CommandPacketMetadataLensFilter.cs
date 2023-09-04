namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(12, 13, 0, ECommandDataType.Utf8String)]
	public class CommandPacketMetadataLensFilter : ICommandPacketBase
	{
		public string Filter; // [0-13]

		public CommandPacketMetadataLensFilter(CommandReader a_reader)
		{
			Filter = a_reader.ReadString();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Filter);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketMetadataLensFilter? other = (CommandPacketMetadataLensFilter?)a_other;
			return other != null &&
			       other.Filter == Filter;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Filter}]";
		}
	}
}
