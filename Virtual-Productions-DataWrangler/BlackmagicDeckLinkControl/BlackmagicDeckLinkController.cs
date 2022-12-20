using System.Runtime.InteropServices;
using BlackmagicCameraControlData;
using DeckLinkAPI;

namespace BlackmagicDeckLinkControl
{
	public class BlackmagicDeckLinkController: CameraControllerBase
	{
		private DeckLinkDeviceNotificationHandler m_deckLinkDeviceNotificationHandler = new DeckLinkDeviceNotificationHandler();
		private IDeckLinkDiscovery m_deckLinkDiscovery = new CDeckLinkDiscoveryClass();
		
		private BlackmagicDeckLinkController()
		{
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
	}
}