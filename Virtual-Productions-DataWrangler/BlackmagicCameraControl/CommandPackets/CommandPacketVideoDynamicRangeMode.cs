using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(1, 7, 1, ECommandDataType.Int8)]
	public class CommandPacketVideoDynamicRangeMode: ICommandPacketBase
	{
		public enum EDynamicRangeMode : byte
		{
			Film,
			Video
		}

		public EDynamicRangeMode Mode;

		public CommandPacketVideoDynamicRangeMode()
		{
		}

		public CommandPacketVideoDynamicRangeMode(CommandReader a_reader)
		{
			Mode = (EDynamicRangeMode)a_reader.ReadInt8();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write((byte)Mode);
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Mode}]";
		}
	}
}
