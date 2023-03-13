namespace BlackmagicCameraControlData.CommandPackets;

[CommandPacketMeta(12, 14, 1, ECommandDataType.Int8)]
public class CommandPacketVendor14 : ICommandPacketBase
{
	public byte Unknown;

	public CommandPacketVendor14(CommandReader a_reader)
	{
		Unknown = a_reader.ReadInt8();
		BlackmagicCameraLogInterface.LogVerbose($"\tReceived Undocumented Packet 12:14, Value {Unknown}");
	}

	public override void WriteTo(CommandWriter a_writer)
	{
		a_writer.Write(Unknown);
	}

	public override bool Equals(ICommandPacketBase? a_other)
	{
		CommandPacketVendor14? other = (CommandPacketVendor14?)a_other;
		return other != null &&
		       other.Unknown == Unknown;
	}

	public override string ToString()
	{
		return $"{GetType().Name} [{Unknown}]";
	}
}