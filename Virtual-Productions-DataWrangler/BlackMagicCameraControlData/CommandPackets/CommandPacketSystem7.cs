using BlackmagicCameraControlData;
using System;
using System.Diagnostics;

namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(9, 7, 12, ECommandDataType.Int16)]
	public class CommandPacketSystem7 : ICommandPacketBase
	{
		public short Unknown = 0; //715		561
		public short Unknown1 = 0; //1950	3000
		public short Unknown2 = 0; //0		0
		public short Unknown3 = 0; //0		0
		public short Unknown4 = 0; //		10000
		public short Unknown5 = 0; //		0

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

			//BlackmagicCameraLogInterface.LogVerbose($"\tReceived Undocumented Packet 9:7, Value [{Unknown}, {Unknown1}, {Unknown2}, {Unknown3}, {Unknown4}, {Unknown5}]");
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

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketSystem7? other = (CommandPacketSystem7?) a_other;
			return other != null &&
			       other.Unknown == Unknown &&
			       other.Unknown1 == Unknown1 &&
			       other.Unknown2 == Unknown2 &&
			       other.Unknown3 == Unknown3 &&
			       other.Unknown4 == Unknown4 &&
			       other.Unknown5 == Unknown5;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Unknown}, {Unknown1}, {Unknown2}, {Unknown3}, {Unknown4}, {Unknown5}]";

		}
	}
}
