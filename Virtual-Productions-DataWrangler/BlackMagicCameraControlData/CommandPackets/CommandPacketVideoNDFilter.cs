using BlackmagicCameraControl;

namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(1, 16, 2, ECommandDataType.Signed5_11FixedPoint)]
	public class CommandPacketVideoNDFilter: ICommandPacketBase
	{
		public Fixed16 Stops = new Fixed16();  //0 - 16.0, F-stop of ND filter to use.

		public CommandPacketVideoNDFilter()
		{
		}

		public CommandPacketVideoNDFilter(CommandReader a_reader)
		{
			Stops = new Fixed16(a_reader.ReadInt16());
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Stops.AsInt16());
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketVideoNDFilter? other = (CommandPacketVideoNDFilter?)a_other;
			return other != null &&
			       other.Stops == Stops;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Stops.AsFloat}]";
		}
	}
}
