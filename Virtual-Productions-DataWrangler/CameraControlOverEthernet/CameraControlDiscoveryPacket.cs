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
}