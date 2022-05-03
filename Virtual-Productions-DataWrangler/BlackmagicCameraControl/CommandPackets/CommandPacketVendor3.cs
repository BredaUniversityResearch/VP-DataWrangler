using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(12, 3, 2, ECommandDataType.Int8)]
	public class CommandPacketVendor3: ICommandPacketBase
	{
		public byte Unknown;	//7 - Seems to increment after every record
		public byte Unknown1;	//255

		public CommandPacketVendor3(CommandReader a_reader)
		{
			Unknown = a_reader.ReadInt8();
			Unknown1 = a_reader.ReadInt8();
		
			IBlackmagicCameraLogInterface.LogVerbose($"\tReceived Undocumented Packet 12:3, Value [{Unknown}, {Unknown1}]");
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Unknown);
			a_writer.Write(Unknown1);
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Unknown}, {Unknown1}]";
		}
	}
}
