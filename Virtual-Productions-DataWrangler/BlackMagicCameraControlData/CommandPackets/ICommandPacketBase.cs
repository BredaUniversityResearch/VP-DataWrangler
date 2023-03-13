namespace BlackmagicCameraControlData.CommandPackets
{
	public abstract class ICommandPacketBase: IEquatable<ICommandPacketBase>
	{
		public abstract void WriteTo(CommandWriter a_writer);
		public abstract bool Equals(ICommandPacketBase? a_other);
	}
}
