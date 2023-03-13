namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(9, 2, 8, ECommandDataType.Int16)]
	public class CommandPacketSystemStorageInfo : ICommandPacketBase
	{
		public short Unknown = 0; 
		public short RecordTimeRemainingSeconds = 0;
		public short Unknown2 = 0;
		public short Unknown3 = 0;

		public CommandPacketSystemStorageInfo()
		{
		}

		public CommandPacketSystemStorageInfo(CommandReader a_reader)
		{
			Unknown = a_reader.ReadInt16();
			RecordTimeRemainingSeconds = a_reader.ReadInt16();
			Unknown2 = a_reader.ReadInt16();
			Unknown3 = a_reader.ReadInt16();

			if (Unknown != 0 || Unknown2 != 0 || Unknown3 != 0)
			{
				BlackmagicCameraLogInterface.LogVerbose(
					$"\tReceived Undocumented Packet 9:2, Value [{Unknown}, {RecordTimeRemainingSeconds}, {Unknown2}, {Unknown3}]");
			}
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Unknown);
			a_writer.Write(RecordTimeRemainingSeconds);
			a_writer.Write(Unknown2);
			a_writer.Write(Unknown3);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketSystemStorageInfo? other = (CommandPacketSystemStorageInfo?)a_other;
			return other != null &&
			       other.Unknown == Unknown &&
			       other.RecordTimeRemainingSeconds == RecordTimeRemainingSeconds &&
			       other.Unknown2 == Unknown2 &&
			       other.Unknown3 == Unknown3;

		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Unknown}, {RecordTimeRemainingSeconds}, {Unknown2}, {Unknown3}]";
		}
	}
}
