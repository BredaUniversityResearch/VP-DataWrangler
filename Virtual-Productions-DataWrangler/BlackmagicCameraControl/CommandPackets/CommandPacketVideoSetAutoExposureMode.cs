using System;

namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(1, 10, 1, ECommandDataType.Int8)]
	public class CommandPacketVideoSetAutoExposureMode : ICommandPacketBase
	{
		public enum EMode: byte
		{
			ManualTrigger = 0,
			Iris = 1,
			Shutter = 2,
			IrisAndShutter = 3,
			ShutterAndIris = 4,
		};

		public EMode Mode = EMode.ManualTrigger;

		public CommandPacketVideoSetAutoExposureMode()
		{
		}

		public CommandPacketVideoSetAutoExposureMode(CommandReader a_reader)
		{
			Mode = (EMode)a_reader.ReadInt8();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write((byte)Mode);
		}
	}
}
