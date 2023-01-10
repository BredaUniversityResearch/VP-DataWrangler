using BlackmagicCameraControlData;
using DeckLinkAPI;

namespace BlackmagicDeckLinkControl;

internal class DeckLinkDeviceNotificationHandler : IDeckLinkDeviceNotificationCallback
{
	private struct DeckLinkHandlePair
	{
		public readonly CameraHandle Handle;
		public readonly IDeckLink DeckLinkInstance;
		public readonly DeckLinkDeviceInputNotificationHandler NotificationHandler;

		public DeckLinkHandlePair(CameraHandle a_handle, IDeckLink a_deckLinkInstance, DeckLinkDeviceInputNotificationHandler a_handler)
		{
			Handle = a_handle;
			DeckLinkInstance = a_deckLinkInstance;
			NotificationHandler = a_handler;
		}
	};

	private BlackmagicDeckLinkController m_controller;
	
	private readonly List<DeckLinkHandlePair> m_activeDevices = new List<DeckLinkHandlePair>();

	public DeckLinkDeviceNotificationHandler(BlackmagicDeckLinkController a_controller)
	{
		m_controller = a_controller;
	}

	public void DeckLinkDeviceArrived(IDeckLink a_deckLinkDevice)
	{
		a_deckLinkDevice.GetModelName(out string name);
		if (a_deckLinkDevice is IDeckLinkProfileAttributes attributes)
		{
			attributes.GetInt(_BMDDeckLinkAttributeID.BMDDeckLinkPersistentID, out long persistent);
		}

		if (a_deckLinkDevice is IDeckLinkInput input)
		{
			DeckLinkDeviceInputNotificationHandler notificationHandler = new DeckLinkDeviceInputNotificationHandler(m_controller, CameraHandleGenerator.Next(), input);
			input.SetCallback(notificationHandler);
			//input.EnableAudioInput(_BMDAudioSampleRate.bmdAudioSampleRate48kHz, _BMDAudioSampleType.bmdAudioSampleType16bitInteger, 2);
			input.EnableVideoInput(_BMDDisplayMode.bmdModeHD1080p25, _BMDPixelFormat.bmdFormat10BitYUV, _BMDVideoInputFlags.bmdVideoInputEnableFormatDetection);
			input.StartStreams();

			DeckLinkHandlePair pair = new DeckLinkHandlePair(notificationHandler.CameraHandle, a_deckLinkDevice, notificationHandler);
			m_activeDevices.Add(pair);

			m_controller.OnDeckLinkDeviceArrived(pair.Handle);
		}
	}

	public void DeckLinkDeviceRemoved(IDeckLink a_deckLinkDevice)
	{
		throw new NotImplementedException();
	}
}