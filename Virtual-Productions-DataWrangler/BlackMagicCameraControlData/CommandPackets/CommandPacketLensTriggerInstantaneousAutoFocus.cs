using BlackmagicCameraControl;

namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(0, 1, 0, ECommandDataType.VoidOrBool)]
	public class CommandPacketLensTriggerInstantaneousAutoFocus : ICommandPacketBase
	{
		public CommandPacketLensTriggerInstantaneousAutoFocus()
		{
		}

		public CommandPacketLensTriggerInstantaneousAutoFocus(CommandReader a_reader)
		{
		}

		public override void WriteTo(CommandWriter a_writer)
		{
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketLensTriggerInstantaneousAutoFocus? other = (CommandPacketLensTriggerInstantaneousAutoFocus?) a_other;
			return other == this;
		}

		public override string ToString()
		{
			return $"{GetType().Name} []";
		}
	}
}
