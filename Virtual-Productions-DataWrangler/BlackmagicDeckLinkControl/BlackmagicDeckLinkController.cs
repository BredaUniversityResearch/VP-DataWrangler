using DeckLinkAPI;

namespace BlackmagicDeckLinkControl
{
	public class BlackmagicDeckLinkController
	{
		class DeviceNotificationCallback : IDeckLinkDeviceNotificationCallback
		{
			public void DeckLinkDeviceArrived(IDeckLink a_deckLinkDevice)
			{
				throw new NotImplementedException();
			}

			public void DeckLinkDeviceRemoved(IDeckLink a_deckLinkDevice)
			{
				throw new NotImplementedException();
			}
		}

		private IDeckLinkDiscovery m_deckLinkDiscovery = new CDeckLinkDiscoveryClass();
		private DeviceNotificationCallback m_deviceNotificationCallback = new DeviceNotificationCallback();

		public BlackmagicDeckLinkController()
		{
			m_deckLinkDiscovery.InstallDeviceNotifications(m_deviceNotificationCallback);
		}
	}
}