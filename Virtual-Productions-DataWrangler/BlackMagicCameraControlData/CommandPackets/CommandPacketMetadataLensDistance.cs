namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(12, 12, 0, ECommandDataType.Utf8String)]
	public class CommandPacketMetadataLensDistance: ICommandPacketBase
	{
		public string Distance;	//[0-49]

		public CommandPacketMetadataLensDistance(CommandReader a_reader)
		{
			Distance = a_reader.ReadString();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Distance);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketMetadataLensDistance? other = (CommandPacketMetadataLensDistance?)a_other;
			return other != null &&
			       other.Distance == Distance;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Distance}]";
		}
	}
}
