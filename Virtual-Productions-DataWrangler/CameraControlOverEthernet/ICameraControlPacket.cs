namespace CameraControlOverEthernet;

internal interface ICameraControlPacket
{
	public void Write(BinaryWriter a_writer);
	public void Read(BinaryReader a_reader);
}