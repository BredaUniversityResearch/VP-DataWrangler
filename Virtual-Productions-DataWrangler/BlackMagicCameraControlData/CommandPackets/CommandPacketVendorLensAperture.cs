namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(12, 10, 0, ECommandDataType.Utf8String)]
	public class CommandPacketVendorAperture : ICommandPacketBase
	{
		public string ApertureSize; //

		public CommandPacketVendorAperture(CommandReader a_reader)
		{
			ApertureSize = a_reader.ReadString();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(ApertureSize);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketVendorAperture? other = (CommandPacketVendorAperture?)a_other;
			return other != null &&
			       other.ApertureSize == ApertureSize;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{ApertureSize}]";
		}
	}
}