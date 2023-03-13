namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(10, 1, 5, ECommandDataType.Int8)]
	public class CommandPacketMediaTransportMode: ICommandPacketBase
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
			SD = 1,
			SSD
		};

		public EMode Mode = EMode.Preview;
		public byte Speed = 0; //- backwards, 0 pause, + forwards 
		public EFlags Flags = 0;
		public EStorageMedium Slot1StorageMedium = EStorageMedium.CFast;
		public EStorageMedium Slot2StorageMedium = EStorageMedium.CFast;

		public CommandPacketMediaTransportMode()
		{
		}

		public CommandPacketMediaTransportMode(CommandReader a_reader)
		{
			Mode = (EMode) a_reader.ReadInt8();
			Speed = a_reader.ReadInt8();
			Flags = (EFlags)a_reader.ReadInt8();
			Slot1StorageMedium = (EStorageMedium)a_reader.ReadInt8();
			Slot2StorageMedium = (EStorageMedium)a_reader.ReadInt8();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write((byte) Mode);
			a_writer.Write(Speed);
			a_writer.Write((byte) Flags);
			a_writer.Write((byte) Slot1StorageMedium);
			a_writer.Write((byte) Slot2StorageMedium);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketMediaTransportMode? other = (CommandPacketMediaTransportMode?)a_other;
			return other != null &&
			       other.Mode == Mode &&
			       other.Speed == Speed &&
			       other.Flags == Flags &&
			       other.Slot1StorageMedium == Slot1StorageMedium &&
			       other.Slot2StorageMedium == Slot2StorageMedium;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Mode}, {Speed}, {Flags}, {Slot1StorageMedium}, {Slot2StorageMedium}]";
		}
	}
}
