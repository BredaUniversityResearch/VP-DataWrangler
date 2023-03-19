using BlackmagicCameraControlData;

namespace CameraControlOverEthernet
{
	public class EthernetRelayCameraController: CameraControllerBase
	{
		private CameraControlNetworkServer m_server = new CameraControlNetworkServer();

		public EthernetRelayCameraController()
		{
			m_server.Start();
		}
	}
}
