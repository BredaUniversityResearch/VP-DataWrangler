using BlackmagicCameraControlData;

namespace BlackmagicCameraControl.CommandPackets;

[CommandPacketMeta(12, 5, 0, ECommandDataType.Utf8String)]
public class CommandPacketVendorCameraNumber : ICommandPacketBase
{
	public string CameraNumber;

	public CommandPacketVendorCameraNumber(CommandReader a_reader)
	{
		CameraNumber = a_reader.ReadString();
	}

	public override void WriteTo(CommandWriter a_writer)
	{
		a_writer.Write(CameraNumber);
	}

	public override bool Equals(ICommandPacketBase? a_other)
	{
		CommandPacketVendorCameraNumber? other = (CommandPacketVendorCameraNumber?)a_other;
		return other != null &&
		       other.CameraNumber == CameraNumber;
	}

	public override string ToString()
	{
		return $"{GetType().Name} [{CameraNumber}]";
	}
}