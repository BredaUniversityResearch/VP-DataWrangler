namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(12, 3, 2, ECommandDataType.Int8)]
	public class CommandPacketMetadataTake: ICommandPacketBase
	{
		public enum ETakeTags: sbyte
		{
			None = -1,
			PU = 0,
			VFX = 1,
			SER = 2
		};

		public sbyte TakeNumber;	//1 - 99
		public ETakeTags TakeTags;	//255

		public CommandPacketMetadataTake(CommandReader a_reader)
		{
			TakeNumber = a_reader.ReadSInt8();
			TakeTags = (ETakeTags)a_reader.ReadSInt8();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(TakeNumber);
			a_writer.Write((sbyte)TakeTags);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketMetadataTake? other = (CommandPacketMetadataTake?)a_other;
			return other != null &&
			       other.TakeNumber == TakeNumber &&
			       other.TakeTags == TakeTags;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{TakeNumber}, {TakeTags}]";
		}
	}
}
