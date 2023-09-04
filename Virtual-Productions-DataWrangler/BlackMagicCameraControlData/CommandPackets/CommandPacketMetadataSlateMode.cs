namespace BlackmagicCameraControlData.CommandPackets;

[CommandPacketMeta(12, 14, 1, ECommandDataType.Int8)]
public class CommandPacketMetadataSlateMode : ICommandPacketBase
{
	public enum ESlateModeType : sbyte
	{
		Recording,
		Playback
	};

	public ESlateModeType Type;

	public CommandPacketMetadataSlateMode(CommandReader a_reader)
	{
		Type = (ESlateModeType)a_reader.ReadSInt8();
	}

	public override void WriteTo(CommandWriter a_writer)
	{
		a_writer.Write((sbyte)Type);
	}

	public override bool Equals(ICommandPacketBase? a_other)
	{
		CommandPacketMetadataSlateMode? other = (CommandPacketMetadataSlateMode?)a_other;
		return other != null &&
		       other.Type == Type;
	}

	public override string ToString()
	{
		return $"{GetType().Name} [{Type}]";
	}
}