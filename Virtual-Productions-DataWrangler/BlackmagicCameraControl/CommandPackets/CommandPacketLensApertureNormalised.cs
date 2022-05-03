namespace BlackmagicCameraControl.CommandPackets
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
	}

}
