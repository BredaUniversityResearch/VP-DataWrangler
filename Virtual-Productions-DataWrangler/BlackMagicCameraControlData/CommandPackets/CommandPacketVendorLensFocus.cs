namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(12, 11, 0, ECommandDataType.Utf8String)]
	public class CommandPacketVendorLensFocus: ICommandPacketBase
	{
		public string FocusDistance;	//

		public CommandPacketVendorLensFocus(CommandReader a_reader)
		{
			FocusDistance = a_reader.ReadString();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(FocusDistance);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketVendorLensFocus? other = (CommandPacketVendorLensFocus?)a_other;
			return other != null &&
			       other.FocusDistance == FocusDistance;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{FocusDistance}]";
		}
	}
}
