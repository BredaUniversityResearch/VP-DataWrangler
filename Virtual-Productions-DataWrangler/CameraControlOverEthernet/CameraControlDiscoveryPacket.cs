using System.Net;

namespace CameraControlOverEthernet;

internal class CameraControlDiscoveryPacket : ICameraControlPacket
{
	public string TargetIp;
	public int TargetPort;

	public CameraControlDiscoveryPacket(IPEndPoint a_connectionListenerLocalEndpoint)
	{
		TargetIp = a_connectionListenerLocalEndpoint.Address.ToString();
		TargetPort = a_connectionListenerLocalEndpoint.Port;
	}
}