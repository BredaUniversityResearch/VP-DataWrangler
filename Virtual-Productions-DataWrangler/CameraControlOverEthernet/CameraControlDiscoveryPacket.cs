using System.Net;

namespace CameraControlOverEthernet;

internal class CameraControlDiscoveryPacket : ICameraControlPacket
{
	public const uint ExpectedMagicBits = 0xA0C0FFEE;
	public uint MagicBits = 0;
	public int ServerIdentifier;
	public int TargetPort;

	public CameraControlDiscoveryPacket(int a_serverIdentifier, int a_serverPort)
	{
		MagicBits = ExpectedMagicBits;
		ServerIdentifier = a_serverIdentifier;
		TargetPort = a_serverPort;
	}

	public void Write(BinaryWriter a_writer)
	{
		a_writer.Write(ExpectedMagicBits);
		a_writer.Write(ServerIdentifier);
		a_writer.Write(TargetPort);
	}

	public void Read(BinaryReader a_reader)
	{
		MagicBits = a_reader.ReadUInt32();
		ServerIdentifier = a_reader.ReadInt32();
		TargetPort = a_reader.ReadInt32();
	}
}