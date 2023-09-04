namespace BlackmagicCameraControlData.CommandPackets;

[CommandPacketMeta(12, 15, 0, ECommandDataType.Utf8String)]
public class CommandPacketMetadataSlateTarget : ICommandPacketBase
{
	public string Name; //[0-31]

	public CommandPacketMetadataSlateTarget(CommandReader a_reader)
	{
		Name = a_reader.ReadString();
	}

	public override void WriteTo(CommandWriter a_writer)
	{
		a_writer.Write(Name);
	}

	public override bool Equals(ICommandPacketBase? a_other)
	{
		CommandPacketMetadataSlateTarget? other = (CommandPacketMetadataSlateTarget?)a_other;
		return other != null &&
		       other.Name == Name;
	}

	public override string ToString()
	{
		return $"{GetType().Name} [{Name}]";
	}
}