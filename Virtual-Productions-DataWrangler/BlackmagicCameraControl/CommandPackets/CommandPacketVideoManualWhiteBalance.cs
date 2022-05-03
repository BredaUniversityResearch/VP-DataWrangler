namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(1, 2, 4, ECommandDataType.Int16)]
	public class CommandPacketVideoManualWhiteBalance: ICommandPacketBase
	{
		public short ColorTemperature = 2500;
		public short Tint = 0;

		public CommandPacketVideoManualWhiteBalance()
		{
		}

		public CommandPacketVideoManualWhiteBalance(CommandReader a_reader)
		{
			ColorTemperature = a_reader.ReadInt16();
			Tint = a_reader.ReadInt16();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(ColorTemperature);
			a_writer.Write(Tint);
		}
	}
}
