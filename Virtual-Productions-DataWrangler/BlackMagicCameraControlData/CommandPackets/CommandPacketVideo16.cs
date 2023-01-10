using BlackmagicCameraControlData;
using System;

namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(1, 16, 2, ECommandDataType.Signed5_11FixedPoint)]
	public class CommandPacketVideo16: ICommandPacketBase
	{
		public Fixed16 Unknown = new Fixed16();  //0

		public CommandPacketVideo16()
		{
		}

		public CommandPacketVideo16(CommandReader a_reader)
		{
			Unknown = new Fixed16(a_reader.ReadInt16());

			BlackmagicCameraLogInterface.LogVerbose($"\tReceived Undocumented Packet 1:16, Value {Unknown.AsFloat}");
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Unknown.AsInt16());
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketVideo16? other = (CommandPacketVideo16?)a_other;
			return other != null &&
			       other.Unknown == Unknown;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Unknown.AsFloat}]";
		}
	}
}
