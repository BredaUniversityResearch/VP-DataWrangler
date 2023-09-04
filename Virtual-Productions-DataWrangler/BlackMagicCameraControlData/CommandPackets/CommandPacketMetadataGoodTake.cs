namespace BlackmagicCameraControlData.CommandPackets;

[CommandPacketMeta(12, 4, 1, ECommandDataType.VoidOrBool)]
public class CommandPacketMetadataGoodTake : ICommandPacketBase
{
	public bool IsGoodTake; //false

	public CommandPacketMetadataGoodTake(CommandReader a_reader)
	{
		IsGoodTake = a_reader.ReadInt8() != 0;
	}

	public override void WriteTo(CommandWriter a_writer)
	{
		a_writer.Write((IsGoodTake)? 1 : 0);
	}

	public override bool Equals(ICommandPacketBase? a_other)
	{
		CommandPacketMetadataGoodTake? other = (CommandPacketMetadataGoodTake?)a_other;
		return other != null &&
		       other.IsGoodTake == IsGoodTake;
	}

	public override string ToString()
	{
		return $"{GetType().Name} [{IsGoodTake}]";
	}
}