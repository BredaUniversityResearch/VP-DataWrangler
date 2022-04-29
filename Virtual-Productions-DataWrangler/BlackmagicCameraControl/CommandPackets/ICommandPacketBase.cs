namespace BlackmagicCameraControl.CommandPackets
{
	public abstract class ICommandPacketBase
	{
		public abstract void WriteTo(CommandWriter a_writer);
	}
}
