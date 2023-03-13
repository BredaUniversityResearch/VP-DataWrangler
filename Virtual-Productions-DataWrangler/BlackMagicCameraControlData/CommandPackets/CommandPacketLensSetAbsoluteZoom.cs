namespace BlackmagicCameraControlData.CommandPackets
{
	[CommandPacketMeta(0, 7, 2, ECommandDataType.Int16)]
	public class CommandPacketLensSetAbsoluteZoom : ICommandPacketBase
	{
		public short Zoom_mm = 0; //In millimeters

		public CommandPacketLensSetAbsoluteZoom()
		{
		}

		public CommandPacketLensSetAbsoluteZoom(CommandReader a_reader)
		{
			Zoom_mm= a_reader.ReadInt16();
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(Zoom_mm);
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketLensSetAbsoluteZoom? other = (CommandPacketLensSetAbsoluteZoom?)a_other;
			return other != null &&
			       other.Zoom_mm == Zoom_mm;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [{Zoom_mm}]";
		}
	}

}
