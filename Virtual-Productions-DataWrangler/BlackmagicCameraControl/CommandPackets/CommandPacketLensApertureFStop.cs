namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(0, 2, 2, ECommandDataType.Signed5_11FixedPoint)]
	public class CommandPacketLensApertureFStop: ICommandPacketBase
	{
		public Fixed16 FStop = new Fixed16();

		public CommandPacketLensApertureFStop()
		{
		}

		public CommandPacketLensApertureFStop(CommandReader a_reader)
		{
			FStop = new Fixed16(a_reader.ReadInt16());
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(FStop.AsInt16());
		}
	}

}
