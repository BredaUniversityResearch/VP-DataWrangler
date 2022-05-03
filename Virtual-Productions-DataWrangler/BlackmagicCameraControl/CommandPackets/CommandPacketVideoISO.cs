namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(1, 14, 4, ECommandDataType.Int32)]
	public class CommandPacketVideoISO: ICommandPacketBase
	{
		public int ISOValue = 0;

		public CommandPacketVideoISO()
		{
		}

		public CommandPacketVideoISO(CommandReader a_reader)
		{
			ISOValue = a_reader.ReadInt32();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(ISOValue);
		}
	}
}
