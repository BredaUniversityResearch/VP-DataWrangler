using System;
using System.Diagnostics;

namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(9, 7, 12, ECommandDataType.Int16)]
	public class CommandPacketSystem7 : ICommandPacketBase
	{
		public short Unknown = 0; //715
		public short Unknown1 = 0; //1950
		public short Unknown2 = 0; //0
		public short Unknown3 = 0; //0
		public short Unknown4 = 0;
		public short Unknown5 = 0;

		public CommandPacketSystem7()
		{
		}

		public CommandPacketSystem7(CommandReader a_reader)
		{
			Unknown = a_reader.ReadInt16();
			Unknown1 = a_reader.ReadInt16();
			Unknown2 = a_reader.ReadInt16();
			Unknown3 = a_reader.ReadInt16();
			Unknown4 = a_reader.ReadInt16();
			Unknown5 = a_reader.ReadInt16();

			IBlackmagicCameraLogInterface.LogVerbose(
				$"\tReceived Undocumented Packet 9:7, Value [{Unknown}, {Unknown1}, {Unknown2}, {Unknown3}, {Unknown4}, {Unknown5}]");
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Unknown);
			a_writer.Write(Unknown1);
			a_writer.Write(Unknown2);
			a_writer.Write(Unknown3);
			a_writer.Write(Unknown4);
			a_writer.Write(Unknown5);
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Unknown}, {Unknown1}, {Unknown2}, {Unknown3}, {Unknown4}, {Unknown5}]";

		}
	}
}
