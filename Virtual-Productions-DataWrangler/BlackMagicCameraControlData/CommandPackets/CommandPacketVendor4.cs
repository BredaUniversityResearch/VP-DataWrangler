namespace BlackmagicCameraControlData.CommandPackets;

[CommandPacketMeta(12, 4, 1, ECommandDataType.VoidOrBool)]
public class CommandPacketVendor4 : ICommandPacketBase
{
	public bool Unknown; //false

	public CommandPacketVendor4(CommandReader a_reader)
	{
		Unknown = a_reader.ReadInt8() != 0;
		BlackmagicCameraLogInterface.LogVerbose($"\tReceived Undocumented Packet 12:4, Value {Unknown}");
	}

	public override void WriteTo(CommandWriter a_writer)
	{
		a_writer.Write((Unknown)? 1 : 0);
	}

	public override bool Equals(ICommandPacketBase? a_other)
	{
		CommandPacketVendor4? other = (CommandPacketVendor4?)a_other;
		return other != null &&
		       other.Unknown == Unknown;
	}

	public override string ToString()
	{
		return $"{GetType().Name} [{Unknown}]";
	}
}