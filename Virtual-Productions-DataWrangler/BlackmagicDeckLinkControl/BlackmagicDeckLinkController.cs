using System.Runtime.InteropServices;
using DeckLinkAPI;

namespace BlackmagicDeckLinkControl
{
	public class BlackmagicDeckLinkController
	{
		class DeviceInputNotificationCallback : IDeckLinkInputCallback
		{
			private const int RemoteControlVANCLineId = 16; // Ursa Mini Manual SDK 1.4, pg 271 "Blanking Encoding"

			public void VideoInputFormatChanged(_BMDVideoInputFormatChangedEvents notificationEvents, IDeckLinkDisplayMode newDisplayMode, _BMDDetectedVideoInputFormatFlags detectedSignalFlags)
			{
				throw new NotImplementedException();
			}

			public void VideoInputFrameArrived(IDeckLinkVideoInputFrame videoFrame, IDeckLinkAudioInputPacket audioPacket)
			{
				int frameWidth = videoFrame.GetWidth();
				int frameHeight = videoFrame.GetHeight();
				videoFrame.GetAncillaryData(out IDeckLinkVideoFrameAncillary ancillaryData);
				_BMDDisplayMode mode = ancillaryData.GetDisplayMode();

				//IntPtr buffer;
				//ancillaryData.GetBufferForVerticalBlankingLine(RemoteControlVANCLineId, out buffer);
				//UnmanagedMemoryStream ms = new UnmanagedMemoryStream(buffer.ToPointer());
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

		private BlackmagicDeckLinkController()
		{
			m_deckLinkDiscovery.InstallDeviceNotifications(m_deviceNotificationCallback);

			if (System.Diagnostics.Debugger.IsAttached)
			{
				Thread.Sleep(new TimeSpan(0, 5, 0));
			}
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