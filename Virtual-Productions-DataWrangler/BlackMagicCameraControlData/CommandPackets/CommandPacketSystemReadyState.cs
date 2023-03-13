namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(9, 1, 2, ECommandDataType.Int8)]
	public class CommandPacketSystemReadyState : ICommandPacketBase
	{
		public byte Unknown = 0; 
		public bool IsReadyToRecord = false;

		public CommandPacketSystemReadyState()
		{
		}

		public CommandPacketSystemReadyState(CommandReader a_reader)
		{
			Unknown = a_reader.ReadInt8();
			IsReadyToRecord = a_reader.ReadInt8() != 0;

			if (Unknown != 0)
			{
				BlackmagicCameraLogInterface.LogVerbose(
					$"\tReceived Undocumented Packet 9:1, Value [{Unknown}, {IsReadyToRecord}]");
			}
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Unknown);
			a_writer.Write(IsReadyToRecord? 1 : 0);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketSystemReadyState? other = (CommandPacketSystemReadyState?)a_other;
			return other != null &&
			       other.Unknown == Unknown &&
			       other.IsReadyToRecord == IsReadyToRecord;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Unknown}, {IsReadyToRecord}]";
		}
	}
}
