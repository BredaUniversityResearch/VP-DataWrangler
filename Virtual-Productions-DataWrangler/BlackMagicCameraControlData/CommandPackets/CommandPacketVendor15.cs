namespace BlackmagicCameraControlData.CommandPackets;

[CommandPacketMeta(12, 15, 0, ECommandDataType.Utf8String)]
public class CommandPacketVendor15 : ICommandPacketBase
{
	public string Unknown;

	public CommandPacketVendor15(CommandReader a_reader)
	{
		Unknown = a_reader.ReadString();
		BlackmagicCameraLogInterface.LogVerbose($"\tReceived Undocumented Packet 12:15, Value {Unknown}");
	}

	public override void WriteTo(CommandWriter a_writer)
	{
		a_writer.Write(Unknown);
	}

	public override bool Equals(ICommandPacketBase? a_other)
	{
		CommandPacketVendor15? other = (CommandPacketVendor15?)a_other;
		return other != null &&
		       other.Unknown == Unknown;
	}

	public override string ToString()
	{
		return $"{GetType().Name} [{Unknown}]";
	}
}