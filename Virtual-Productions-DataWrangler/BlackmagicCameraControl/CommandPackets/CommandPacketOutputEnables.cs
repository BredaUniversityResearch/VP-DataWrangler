using System;
using System.Diagnostics;

namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(3, 0, 2, ECommandDataType.Int16)]
	public class CommandPacketOutputEnables: ICommandPacketBase
	{
		[Flags]
		public enum EOverlayFlags
		{
			Status = (1 << 0),
			FrameGuides = (1 <<1)
		};

		public EOverlayFlags EnabledOverlays = 0;

		public CommandPacketOutputEnables()
		{
		}

		public CommandPacketOutputEnables(CommandReader a_reader)
		{
			EnabledOverlays = (EOverlayFlags)a_reader.ReadInt16();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write((short)EnabledOverlays);
		}
	}
}