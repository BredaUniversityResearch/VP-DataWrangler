using System;
using System.Diagnostics;

namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(9, 1, 2, ECommandDataType.Int8)]
	public class CommandPacketBattery1 : ICommandPacketBase
	{
		public byte Unknown = 0; 
		public byte Unknown1 = 0;

		public CommandPacketBattery1()
		{
		}

		public CommandPacketBattery1(CommandReader a_reader)
		{
			Unknown = a_reader.ReadInt8();
			Unknown1 = a_reader.ReadInt8();
			
			IBlackmagicCameraLogInterface.LogVerbose($"\tReceived Undocumented Packet 9:1, Value [{Unknown}, {Unknown1} (Ready?)]");
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Unknown);
			a_writer.Write(Unknown1);
		}
	}
}
