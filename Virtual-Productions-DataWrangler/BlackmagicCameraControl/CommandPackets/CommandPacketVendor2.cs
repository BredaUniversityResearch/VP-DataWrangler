namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(12, 2, 0, ECommandDataType.Utf8String)]
	public class CommandPacketVendor2: ICommandPacketBase
	{
		public string Unknown;	//

		public CommandPacketVendor2(CommandReader a_reader)
		{
			Unknown = a_reader.ReadString();

			IBlackmagicCameraLogInterface.LogVerbose($"\tReceived Undocumented Packet 12:2, Value [{Unknown}]");
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Unknown);
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Unknown}]";
		}
	}
}
