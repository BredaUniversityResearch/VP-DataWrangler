using System;
using System.Diagnostics;

namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(9, 2, 8, ECommandDataType.Int16)]
	public class CommandPacketBattery2 : ICommandPacketBase
	{
		public short Unknown = 0; 
		public short Unknown1 = 0;
		public short Unknown2 = 0;
		public short Unknown3 = 0;

		public CommandPacketBattery2()
		{
		}

		public CommandPacketBattery2(CommandReader a_reader)
		{
			Unknown = a_reader.ReadInt16();
			Unknown1 = a_reader.ReadInt16();
			Unknown2 = a_reader.ReadInt16();
			Unknown3 = a_reader.ReadInt16();

			IBlackmagicCameraLogInterface.LogVerbose($"\tReceived Undocumented Packet 9:2, Value [{Unknown}, {Unknown1} (Seconds Recording Remaining?), {Unknown2}, {Unknown3}]");
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Unknown);
			a_writer.Write(Unknown1);
			a_writer.Write(Unknown2);
			a_writer.Write(Unknown3);
		}
	}
}
