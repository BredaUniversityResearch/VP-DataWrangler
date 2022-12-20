using BlackmagicCameraControlData;

namespace BlackmagicCameraControl.CommandPackets;

[CommandPacketMeta(12, 5, 0, ECommandDataType.Utf8String)]
public class CommandPacketVendor5 : ICommandPacketBase
{
	public string Unknown;

	public CommandPacketVendor5(CommandReader a_reader)
	{
		Unknown = a_reader.ReadString();
		BlackmagicCameraLogInterface.LogVerbose($"\tReceived Undocumented Packet 12:5, Value {Unknown}");
	}

	public override void WriteTo(CommandWriter a_writer)
	{
		a_writer.Write(Unknown);
	}

	public override string ToString()
	{
		return $"{GetType().Name} [{Unknown}]";
	}
}