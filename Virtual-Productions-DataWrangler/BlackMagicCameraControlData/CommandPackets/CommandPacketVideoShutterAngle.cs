﻿namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(1, 11, 4, ECommandDataType.Int32)]
	public class CommandPacketVideoShutterAngle : ICommandPacketBase
	{
		public int Angle = 100; //Shutter angle multiplied by 100; Range: [100..36000]

		public CommandPacketVideoShutterAngle()
		{
		}

		public CommandPacketVideoShutterAngle(CommandReader a_reader)
		{
			Angle = a_reader.ReadInt32();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Angle);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketVideoShutterAngle? other = (CommandPacketVideoShutterAngle?)a_other;
			return other != null &&
			       other.Angle == Angle;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Angle}]";
		}
	}
}
