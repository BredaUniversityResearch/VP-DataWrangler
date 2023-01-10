namespace BlackmagicCameraControl.CommandPackets
{
	[CommandPacketMeta(0, 2, 4, ECommandDataType.Signed5_11FixedPoint)]
	public class CommandPacketLensApertureFStop: ICommandPacketBase
	{
		public Fixed16 FStop = new Fixed16();
		public Fixed16 Unknown = new Fixed16();

		public CommandPacketLensApertureFStop()
		{
		}

		public CommandPacketLensApertureFStop(CommandReader a_reader)
		{
			FStop = new Fixed16(a_reader.ReadInt16());
			Unknown = new Fixed16(a_reader.ReadInt16());
		}

		public override void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(FStop.AsInt16());
			a_writer.Write(Unknown.AsInt16());
		}

		public override bool Equals(ICommandPacketBase? a_other)
		{
			CommandPacketLensApertureFStop? other = (CommandPacketLensApertureFStop?) a_other;
			return other != null &&
			       other.FStop == FStop &&
			       other.Unknown == Unknown;
		}

		public override string ToString()
		{
			return $"{GetType().Name} [FStop: {FStop.AsFloat} Unknown: {Unknown.AsFloat}]";
		}
	}
}
