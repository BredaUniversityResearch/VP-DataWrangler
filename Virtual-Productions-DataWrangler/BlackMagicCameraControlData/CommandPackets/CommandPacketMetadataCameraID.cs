namespace BlackmagicCameraControlData.CommandPackets;

[CommandPacketMeta(12, 5, 0, ECommandDataType.Utf8String)]
public class CommandPacketMetadataCameraID : ICommandPacketBase
{
	public string ID; // [0-28]

	public CommandPacketMetadataCameraID(CommandReader a_reader)
	{
		ID = a_reader.ReadString();
	}

	public override void WriteTo(CommandWriter a_writer)
	{
		a_writer.Write(ID);
	}

	public override bool Equals(ICommandPacketBase? a_other)
	{
		CommandPacketMetadataCameraID? other = (CommandPacketMetadataCameraID?)a_other;
		return other != null &&
		       other.ID == ID;
	}

	public override string ToString()
	{
		return $"{GetType().Name} [{ID}]";
	}
}