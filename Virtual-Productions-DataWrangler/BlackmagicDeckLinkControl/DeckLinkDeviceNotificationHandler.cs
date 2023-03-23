using System.Runtime.InteropServices;
using BlackmagicCameraControlData;
using CommonLogging;
using DeckLinkAPI;

namespace BlackmagicDeckLinkControl;

internal class DeckLinkDeviceNotificationHandler : IDeckLinkDeviceNotificationCallback
{
	private struct DeckLinkHandlePair
	{
		public readonly CameraDeviceHandle DeviceHandle;
		public readonly IDeckLink DeckLinkInstance;
		public readonly DeckLinkDeviceInputNotificationHandler NotificationHandler;

		public DeckLinkHandlePair(CameraDeviceHandle a_deviceHandle, IDeckLink a_deckLinkInstance, DeckLinkDeviceInputNotificationHandler a_handler)
		{
			DeviceHandle = a_deviceHandle;
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
	
        if (a_deckLinkDevice is IDeckLinkConfiguration config)
        {
			config.SetInt(_BMDDeckLinkConfigurationID.bmdDeckLinkConfigVideoInputConnection, (int)_BMDVideoConnection.bmdVideoConnectionSDI);
        }

		if (a_deckLinkDevice is IDeckLinkInput input)
		{
			long decklinkPersistentId = 0;
            if (a_deckLinkDevice is IDeckLinkProfileAttributes attributes)
            {
	            attributes.GetInt(_BMDDeckLinkAttributeID.BMDDeckLinkPersistentID, out decklinkPersistentId);

	            attributes.GetFlag(_BMDDeckLinkAttributeID.BMDDeckLinkVANCRequires10BitYUVVideoFrames, out int requires10BitYuv);
	            string required = (requires10BitYuv != 0) ? "" : "not";
	            Logger.LogInfo("DeckLinkInterface", $"DeckLink Hardware reports 10BitYUV as {required} required for VANC data");
 
            }
			DeckLinkDeviceInputNotificationHandler notificationHandler = new DeckLinkDeviceInputNotificationHandler(new CameraDeviceHandle($"DeckLink#{decklinkPersistentId}", m_controller), input);
            input.SetCallback(notificationHandler);
			//We need audio for VANC
			input.EnableAudioInput(_BMDAudioSampleRate.bmdAudioSampleRate48kHz, _BMDAudioSampleType.bmdAudioSampleType16bitInteger, 2);
            try
            {
                notificationHandler.StartVideoInput();
            }
            catch (COMException ex)
            {
				Logger.LogError("DeckLinkInterface", $"Failed to start video input. Exception: {ex.Message}");
            }


			input.StartStreams();

			DeckLinkHandlePair pair = new DeckLinkHandlePair(notificationHandler.CameraDeviceHandle, a_deckLinkDevice, notificationHandler);
			m_activeDevices.Add(pair);

			m_controller.OnDeckLinkDeviceArrived(pair.DeviceHandle);
		}
	}

	public void DeckLinkDeviceRemoved(IDeckLink a_deckLinkDevice)
	{
		throw new NotImplementedException();
	}
}