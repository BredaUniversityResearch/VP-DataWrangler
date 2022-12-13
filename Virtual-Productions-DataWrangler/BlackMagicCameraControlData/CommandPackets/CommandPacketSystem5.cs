using System;
using System.Diagnostics;

namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(9, 5, 4, ECommandDataType.Int8)]
	public class CommandPacketSystem5 : ICommandPacketBase
	{
		public byte Unknown = 0;	//0
		public byte Unknown1 = 0;	//16
		public byte Unknown2 = 0;	//0
		public byte Unknown3 = 0;	//0

		public CommandPacketSystem5()
		{
		}

		public CommandPacketSystem5(CommandReader a_reader)
		{
			Unknown = a_reader.ReadInt8();
			Unknown1 = a_reader.ReadInt8();
			Unknown2 = a_reader.ReadInt8();
			Unknown3 = a_reader.ReadInt8();

			IBlackmagicCameraLogInterface.LogVerbose($"\tReceived Undocumented Packet 9:5, Value [{Unknown}, {Unknown1}, {Unknown2}, {Unknown3}]");

		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Unknown);
			a_writer.Write(Unknown1);
			a_writer.Write(Unknown2);
			a_writer.Write(Unknown3);
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Unknown}, {Unknown1}, {Unknown2}, {Unknown3}]";
		}
	}
}
