namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(12, 8, 0, ECommandDataType.Utf8String)]
	public class CommandPacketMetadataProjectName: ICommandPacketBase
	{
		public string ProjectName; // [0-28]

		public CommandPacketMetadataProjectName(CommandReader a_reader)
		{
			ProjectName = a_reader.ReadString();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(ProjectName);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketMetadataProjectName? other = (CommandPacketMetadataProjectName?)a_other;
			return other != null &&
			       other.ProjectName == ProjectName;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{ProjectName}]";
		}
	}
}
