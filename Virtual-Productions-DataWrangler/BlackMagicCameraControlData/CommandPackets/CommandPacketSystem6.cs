using BlackmagicCameraControlData;
using System;
using System.Diagnostics;

namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(9, 6, 2, ECommandDataType.Int8)]
	public class CommandPacketSystem6 : ICommandPacketBase
	{
		public short Unknown = 0; //0 
		public short Unknown1 = 0; //0 / 1 ? Tally?

		public CommandPacketSystem6()
		{
		}

		public CommandPacketSystem6(CommandReader a_reader)
		{
			Unknown = a_reader.ReadInt8();
			Unknown1 = a_reader.ReadInt8();

			//BlackmagicCameraLogInterface.LogVerbose($"\tReceived Undocumented Packet 9:6, Value [{Unknown}, {Unknown1}]");
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Unknown);
			a_writer.Write(Unknown1);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketSystem6? other = (CommandPacketSystem6?)a_other;
			return other != null &&
			       other.Unknown == Unknown &&
			       other.Unknown1 == Unknown1;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Unknown}, {Unknown1}]";

		}
	}
}
