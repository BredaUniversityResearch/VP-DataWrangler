namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(1, 15, 2, ECommandDataType.Int8)]
	public class CommandPacketVideoDisplayLUT : ICommandPacketBase
	{
		public byte SelectedLUT = 0; // 0 - None, 1 - Custom, 2 - Film to Video, 3- Film to Extended Video
		public bool IsEnabled = false;

		public CommandPacketVideoDisplayLUT()
		{
		}

		public CommandPacketVideoDisplayLUT(CommandReader a_reader)
		{
			SelectedLUT = a_reader.ReadInt8();
			IsEnabled = (a_reader.ReadInt8() != 0);
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(SelectedLUT);
			a_writer.Write((IsEnabled)? 0 : 1);
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{SelectedLUT}, {IsEnabled}]";
		}
	}
}