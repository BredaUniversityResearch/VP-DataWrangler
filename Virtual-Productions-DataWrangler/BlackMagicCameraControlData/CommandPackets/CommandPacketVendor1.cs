using BlackmagicCameraControlData;

namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(12, 1, 3, ECommandDataType.Int8)]
	public class CommandPacketVendor1 : ICommandPacketBase
	{
		public byte Unknown;	//
		public byte Unknown1;	//
		public byte Unknown2;	//

		public CommandPacketVendor1(CommandReader a_reader)
		{
			Unknown = a_reader.ReadInt8();
			Unknown1 = a_reader.ReadInt8();
			Unknown2 = a_reader.ReadInt8();

			BlackmagicCameraLogInterface.LogVerbose($"\tReceived Undocumented Packet 12:1, Value [{Unknown}, {Unknown1}, {Unknown2}]");
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Unknown);
			a_writer.Write(Unknown1);
			a_writer.Write(Unknown2);
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Unknown}, {Unknown1}, {Unknown2}]";
		}
	}
}
