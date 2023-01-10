namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(10, 0, 2, ECommandDataType.Int8)]
	public class CommandPacketMediaCodec: ICommandPacketBase
	{
		public enum EBasicCodec: byte
		{
			Raw = 0,
			DNxHD = 1,
			ProRes = 2,
			BlackmagicRAW = 3
		};

		public EBasicCodec BasicCodec = EBasicCodec.Raw;
		public byte Variant = 0;

		public CommandPacketMediaCodec()
		{
		}

		public CommandPacketMediaCodec(CommandReader a_reader)
		{
			BasicCodec = (EBasicCodec) a_reader.ReadInt8();
			Variant = a_reader.ReadInt8();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write((byte) BasicCodec);
			a_writer.Write(Variant);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketMediaCodec? other = (CommandPacketMediaCodec?)a_other;
			return other != null &&
			       other.BasicCodec == BasicCodec &&
			       other.Variant == Variant;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{BasicCodec}, {Variant}]";
		}
	}
}
