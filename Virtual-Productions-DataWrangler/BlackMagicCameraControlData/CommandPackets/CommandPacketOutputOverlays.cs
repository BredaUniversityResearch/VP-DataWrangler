using System;
using System.Diagnostics;

namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(3, 3, 4, ECommandDataType.Int8)]
	public class CommandPacketOutputOverlays : ICommandPacketBase
	{
		public enum EFrameGuideStyle: byte
		{
			FG_Off = 0,
			FG_2_4By1 = 1,
			FG_2_39By1 = 2,
			FG_2_35By1 = 3,
			FG_1_85By1 = 4,
			FG_16By9 = 5,
			FG_14By9 = 6,
			FG_4By3 = 7,
			FG_2By1 = 8,
			FG_4By5 = 9,
			FG_1By1 = 10
		};

		[Flags]
		public enum EGridStyleFlags
		{
			Thirds = (1 << 0),
			CrossHairs = (1 << 1),
			CenterDot = (1 << 2),
			Horizon = (1 << 3)
		};

		public EFrameGuideStyle Style = EFrameGuideStyle.FG_Off;
		public byte Opacity = 0; // [0..100], 100 = opaque
		public byte SafeArea = 0; //[0..100], 0 - off, percentage
		public EGridStyleFlags Grid = 0;


		public CommandPacketOutputOverlays()
		{
		}

		public CommandPacketOutputOverlays(CommandReader a_reader)
		{
			Style = (EFrameGuideStyle)a_reader.ReadInt8();
			Opacity = a_reader.ReadInt8();
			SafeArea = a_reader.ReadInt8();
			Grid = (EGridStyleFlags)a_reader.ReadInt8();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write((byte)Style);
			a_writer.Write(Opacity);
			a_writer.Write(SafeArea);
			a_writer.Write((byte)Grid);
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Style}, {Opacity}, {SafeArea}, {Grid}]";
		}
	}
}