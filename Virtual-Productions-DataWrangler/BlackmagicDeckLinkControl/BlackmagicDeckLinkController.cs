using DeckLinkAPI;

namespace BlackmagicDeckLinkControl
{
	public class BlackmagicDeckLinkController
	{
		class DeviceInputNotificationCallback : IDeckLinkInputCallback
		{
			public void VideoInputFormatChanged(_BMDVideoInputFormatChangedEvents notificationEvents, IDeckLinkDisplayMode newDisplayMode, _BMDDetectedVideoInputFormatFlags detectedSignalFlags)
			{
				throw new NotImplementedException();
			}

			public void VideoInputFrameArrived(IDeckLinkVideoInputFrame videoFrame, IDeckLinkAudioInputPacket audioPacket)
			{
				throw new NotImplementedException();
			}
		}

		class DeviceNotificationCallback : IDeckLinkDeviceNotificationCallback
		{
			private DeviceInputNotificationCallback m_deviceInputNotificationCallback = new DeviceInputNotificationCallback();

			public void DeckLinkDeviceArrived(IDeckLink a_deckLinkDevice)
			{
				a_deckLinkDevice.GetModelName(out string name);
				if (a_deckLinkDevice is IDeckLinkProfileAttributes attributes)
				{
					attributes.GetInt(_BMDDeckLinkAttributeID.BMDDeckLinkPersistentID, out long persistent);
					int a = 6;
				}

				if (a_deckLinkDevice is IDeckLinkInput input)
				{
					input.SetCallback(m_deviceInputNotificationCallback);
					input.EnableVideoInput(_BMDDisplayMode.bmdModeHD1080p6000, _BMDPixelFormat.bmdFormat8BitARGB, _BMDVideoInputFlags.bmdVideoInputFlagDefault);
					input.StartStreams();
				}
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

			if (System.Diagnostics.Debugger.IsAttached)
			{
				Thread.Sleep(new TimeSpan(0, 5, 0));
			}
		}
	}
}