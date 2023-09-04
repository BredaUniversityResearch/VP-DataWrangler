namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(12, 6, 0, ECommandDataType.Utf8String)]
	public class CommandPacketMetadataCameraOperator: ICommandPacketBase
	{
		public string Operator; // [0-28]

		public CommandPacketMetadataCameraOperator(CommandReader a_reader)
		{
			Operator = a_reader.ReadString();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Operator);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketMetadataCameraOperator? other = (CommandPacketMetadataCameraOperator?)a_other;
			return other != null &&
			       other.Operator == Operator;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Operator}]";
		}
	}
}
