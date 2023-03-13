using BlackmagicCameraControl;

namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(0, 3, 2, ECommandDataType.Signed5_11FixedPoint)]
	public class CommandPacketLensApertureNormalised : ICommandPacketBase
	{
		public Fixed16 FStopNormalised = new Fixed16();

		public CommandPacketLensApertureNormalised()
		{
		}

		public CommandPacketLensApertureNormalised(CommandReader a_reader)
		{
			FStopNormalised = new Fixed16(a_reader.ReadInt16());
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(FStopNormalised.AsInt16());
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketLensApertureNormalised? other = (CommandPacketLensApertureNormalised?)a_other;
			return other != null &&
			       other.FStopNormalised == FStopNormalised;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{FStopNormalised.AsFloat}]";
		}
	}
}
