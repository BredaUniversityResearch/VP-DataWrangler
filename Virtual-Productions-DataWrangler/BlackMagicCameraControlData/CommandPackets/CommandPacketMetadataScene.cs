namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(12, 2, 0, ECommandDataType.Utf8String)]
	public class CommandPacketMetadataScene: ICommandPacketBase
	{
		public string Scene;	//

		public CommandPacketMetadataScene(CommandReader a_reader)
		{
			Scene = a_reader.ReadString();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Scene);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketMetadataScene? other = (CommandPacketMetadataScene?)a_other;
			return other != null &&
			       other.Scene == Scene;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Scene}]";
		}
	}
}
