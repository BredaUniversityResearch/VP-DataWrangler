namespace CameraControlOverEthernet;

public interface INetworkAPIEventHandler
{
	void OnClientConnected(int a_connectionId, NetworkAPIDeviceCapabilities a_deviceCapabilities);
	void OnClientDisconnected(int a_connectionId);
	void OnPacketReceived(INetworkAPIPacket a_packet, int a_connectionId);
}