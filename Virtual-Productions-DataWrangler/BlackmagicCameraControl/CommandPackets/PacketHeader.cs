namespace BlackmagicCameraControl.CommandPackets
{
	public class PacketHeader
	{
		public const long ByteSize = 4;

		public byte TargetCamera = 255; //255 = broadcast.
		public byte PacketSize = 0;
		public EPacketCommand Command = EPacketCommand.ChangeConfig;
		public byte Reserved = 0;

		public void WriteTo(CommandWriter a_writer)
		{
			a_writer.Write(TargetCamera);
			a_writer.Write(PacketSize);
			a_writer.Write((byte)Command);
			a_writer.Write(Reserved);
		}
	}
}
