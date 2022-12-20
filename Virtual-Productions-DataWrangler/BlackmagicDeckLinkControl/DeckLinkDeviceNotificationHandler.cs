using DeckLinkAPI;

namespace BlackmagicDeckLinkControl;

internal class DeckLinkDeviceNotificationHandler : IDeckLinkDeviceNotificationCallback
{
	private DeckLinkDeviceInputNotificationHandler? m_deviceInputNotificationCallback = null;

	public void DeckLinkDeviceArrived(IDeckLink a_deckLinkDevice)
	{
		a_deckLinkDevice.GetModelName(out string name);
		if (a_deckLinkDevice is IDeckLinkProfileAttributes attributes)
		{
			attributes.GetInt(_BMDDeckLinkAttributeID.BMDDeckLinkPersistentID, out long persistent);
		}

		if (a_deckLinkDevice is IDeckLinkInput input)
		{
			if (m_deviceInputNotificationCallback != null)
			{
				throw new Exception("Input device not properly released");
			}

			m_deviceInputNotificationCallback = new DeckLinkDeviceInputNotificationHandler(input);
			input.SetCallback(m_deviceInputNotificationCallback);
			//input.EnableAudioInput(_BMDAudioSampleRate.bmdAudioSampleRate48kHz, _BMDAudioSampleType.bmdAudioSampleType16bitInteger, 2);
			input.EnableVideoInput(_BMDDisplayMode.bmdModeHD1080p25, _BMDPixelFormat.bmdFormat10BitYUV, _BMDVideoInputFlags.bmdVideoInputEnableFormatDetection);
			input.StartStreams();
		}
	}

	public void DeckLinkDeviceRemoved(IDeckLink a_deckLinkDevice)
	{
		throw new NotImplementedException();
	}
}