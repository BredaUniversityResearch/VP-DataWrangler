using System.Runtime.InteropServices;
using BlackmagicCameraControl.CommandPackets;
using BlackmagicCameraControlData;
using BlackmagicCameraControlData.CommandPackets;
using DeckLinkAPI;

namespace BlackmagicDeckLinkControl
{
	public class BlackmagicDeckLinkController: CameraControllerBase
	{
		private DeckLinkDeviceNotificationHandler m_deckLinkDeviceNotificationHandler;
		private IDeckLinkDiscovery m_deckLinkDiscovery = new CDeckLinkDiscoveryClass();

		private Dictionary<CameraHandle, CameraPropertyCache> m_activeCameras = new Dictionary<CameraHandle, CameraPropertyCache>();

		private BlackmagicDeckLinkController()
		{
			m_deckLinkDeviceNotificationHandler = new DeckLinkDeviceNotificationHandler(this);
			m_deckLinkDiscovery.InstallDeviceNotifications(m_deckLinkDeviceNotificationHandler);
		}

		public static BlackmagicDeckLinkController? Create(out string? a_errorMessage)
		{
			a_errorMessage = null;
			try
			{
				return new BlackmagicDeckLinkController();
			}
			catch (COMException ex)
			{
				a_errorMessage = $"COM Exception, DeckLink API Components not found. {ex.Message}";
				return null;
			}
		}

		public void OnDeckLinkDeviceArrived(CameraHandle a_handle)
		{
			m_activeCameras.Add(a_handle, new CameraPropertyCache());
			CameraConnected(a_handle);
		}

		public void OnCameraDeviceRemoved(CameraHandle a_handle)
		{
			throw new NotImplementedException();
		}

		public void OnCameraPacketArrived(CameraHandle a_handle, CommandIdentifier a_id, ICommandPacketBase a_packet)
		{
			if (m_activeCameras.TryGetValue(a_handle, out CameraPropertyCache? cache))
			{
				if (cache.CheckPropertyChanged(a_id, a_packet))
				{
					CameraDataReceived(a_handle, DateTimeOffset.UtcNow, a_packet);
				}
			}
		}
	}
}