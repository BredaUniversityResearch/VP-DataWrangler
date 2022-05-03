using System;

namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(1, 9, 10, ECommandDataType.Int16)]
	public class CommandPacketVideoRecordingFormat : ICommandPacketBase
	{
		[Flags]
		public enum EFlags: short
		{
			FileMRate = (1 << 0),
			SensorMRate = (1 << 1),
			SensorOffSpeed = (1 << 2),
			Interlaced = (1 << 3),
			WindowedMode = (1 << 4)
		};

		public short FileFrameRate;
		public short SensorFrameRate;
		public short FrameWidth;
		public short FrameHeight;
		public EFlags Flags = 0;

		public CommandPacketVideoRecordingFormat()
		{
		}

		public CommandPacketVideoRecordingFormat(CommandReader a_reader)
		{
			FileFrameRate = a_reader.ReadInt16();
			SensorFrameRate = a_reader.ReadInt16();
			FrameWidth = a_reader.ReadInt16();
			FrameHeight = a_reader.ReadInt16();
			Flags = (EFlags)a_reader.ReadInt16();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(FileFrameRate);
			a_writer.Write(SensorFrameRate);
			a_writer.Write(FrameWidth);
			a_writer.Write(FrameHeight);
			a_writer.Write((short)Flags);
		}
	}
}
