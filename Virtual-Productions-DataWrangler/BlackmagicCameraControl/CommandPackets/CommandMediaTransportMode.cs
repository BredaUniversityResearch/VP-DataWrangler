using System;

namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(10, 1, 4, ECommandDataType.Int8)]
	public class CommandMediaTransportMode: ICommandPacketBase
	{
		public enum EMode : byte
		{
			Preview,
			Play,
			Record
		}

		[Flags]
		public enum EFlags : byte
		{
			Loop = (1 << 0),
			PlayAll = (1 << 1),
			Disk1Active = (1 << 5),
			Disk2Active = (1 << 6),
			TimeLapseRecording = (1 << 7)
		}

		public enum EStorageMedium : byte
		{
			CFast = 0,
			SD = 1
		};

		public EMode Mode = EMode.Preview;
		public byte Speed = 0; //- backwards, 0 pause, + forwards 
		public EFlags Flags = 0;
		public EStorageMedium ActiveStorage = EStorageMedium.CFast;

		public CommandMediaTransportMode()
		{
		}

		public CommandMediaTransportMode(CommandReader a_reader)
		{
			Mode = (EMode) a_reader.ReadInt8();
			Speed = a_reader.ReadInt8();
			Flags = (EFlags)a_reader.ReadInt8();
			ActiveStorage = (EStorageMedium)a_reader.ReadInt8();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write((byte) Mode);
			a_writer.Write(Speed);
			a_writer.Write((byte) Flags);
			a_writer.Write((byte) ActiveStorage);
		}
	}
}
