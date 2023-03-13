namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(9, 6, 2, ECommandDataType.Int8)]
	public class CommandPacketSystemAtemSetup : ICommandPacketBase
	{
		public short Unknown = 0; //0 
		public short ATEMCameraId = 0; //0 / 1 ? Tally?

		public CommandPacketSystemAtemSetup()
		{
		}

		public CommandPacketSystemAtemSetup(CommandReader a_reader)
		{
			Unknown = a_reader.ReadInt8();
			ATEMCameraId = a_reader.ReadInt8();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Unknown);
			a_writer.Write(ATEMCameraId);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketSystemAtemSetup? other = (CommandPacketSystemAtemSetup?)a_other;
			return other != null &&
			       other.Unknown == Unknown &&
			       other.ATEMCameraId == ATEMCameraId;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Unknown}, {ATEMCameraId}]";

		}
	}
}
