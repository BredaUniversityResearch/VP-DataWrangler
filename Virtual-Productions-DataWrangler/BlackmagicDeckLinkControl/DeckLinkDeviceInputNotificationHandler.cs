using System.Diagnostics;
using BlackmagicCameraControl.CommandPackets;
using BlackmagicCameraControlData;
using CommonLogging;
using DeckLinkAPI;

namespace BlackmagicDeckLinkControl;

internal class DeckLinkDeviceInputNotificationHandler : IDeckLinkInputCallback
{
    // Ursa Mini Manual SDK 1.4, pg 271 "Blanking Encoding"
    private const byte DIDPacketBlackmagic = 0x51;
    private const byte SDIDTally = 0x52;

    private const byte SDIDCameraControl = 0x53;
    //private const int RemoteControlVANCLineId = 16;

    private readonly BlackmagicDeckLinkController m_controller;
    public readonly CameraHandle CameraHandle;
    private readonly IDeckLinkInput m_targetDevice;
    private _BMDDisplayMode m_targetDisplayMode = _BMDDisplayMode.bmdModeHD1080p25;
    private _BMDPixelFormat m_targetPixelFormat = _BMDPixelFormat.bmdFormat8BitARGB;

    public DeckLinkDeviceInputNotificationHandler(BlackmagicDeckLinkController a_controller,
        CameraHandle a_cameraHandle, IDeckLinkInput a_targetDevice)
    {
        m_controller = a_controller;
        CameraHandle = a_cameraHandle;
        m_targetDevice = a_targetDevice;
    }

    public void VideoInputFormatChanged(_BMDVideoInputFormatChangedEvents a_notificationEvents,
        IDeckLinkDisplayMode a_newDisplayMode, _BMDDetectedVideoInputFormatFlags a_detectedSignalFlags)
    {
        _BMDDisplayMode targetMode = m_targetDisplayMode;
        _BMDPixelFormat pixelFormat = m_targetPixelFormat;

        if ((a_notificationEvents & _BMDVideoInputFormatChangedEvents.bmdVideoInputDisplayModeChanged) != 0)
        {
            targetMode = a_newDisplayMode.GetDisplayMode();
        }

        if ((a_notificationEvents & _BMDVideoInputFormatChangedEvents.bmdVideoInputColorspaceChanged) != 0)
        {
	        bool isRGB = (a_detectedSignalFlags & _BMDDetectedVideoInputFormatFlags.bmdDetectedVideoInputRGB444) != 0;

	        if ((a_detectedSignalFlags & _BMDDetectedVideoInputFormatFlags.bmdDetectedVideoInput8BitDepth) != 0)
	        {
		        pixelFormat = (isRGB)? _BMDPixelFormat.bmdFormat8BitARGB : _BMDPixelFormat.bmdFormat8BitYUV;
	        }
            else if ((a_detectedSignalFlags & _BMDDetectedVideoInputFormatFlags.bmdDetectedVideoInput10BitDepth) != 0)
	        {
		        pixelFormat = (isRGB)? _BMDPixelFormat.bmdFormat10BitRGB : _BMDPixelFormat.bmdFormat10BitYUV;
	        }
			else if ((a_detectedSignalFlags & _BMDDetectedVideoInputFormatFlags.bmdDetectedVideoInput12BitDepth) != 0)
	        {
		        if (!isRGB)
			        throw new Exception("Invalid pixel format received, rgb & 12-bit");
		        pixelFormat = _BMDPixelFormat.bmdFormat12BitRGB;
	        }
		}

        if (m_targetDisplayMode != targetMode || m_targetPixelFormat != pixelFormat)
        {
            m_targetDisplayMode = targetMode;
            m_targetPixelFormat = pixelFormat;

            m_targetDevice.PauseStreams();
            m_targetDevice.EnableVideoInput(targetMode, pixelFormat, _BMDVideoInputFlags.bmdVideoInputFlagDefault);
            m_targetDevice.FlushStreams();
            m_targetDevice.StartStreams();
        }
    }

    public void VideoInputFrameArrived(IDeckLinkVideoInputFrame videoFrame, IDeckLinkAudioInputPacket audioPacket)
    {
        _BMDFrameFlags flags = videoFrame.GetFlags();
        if ((flags & _BMDFrameFlags.bmdFrameHasNoInputSource) != 0)
        {
            return;
        }

        int frameWidth = videoFrame.GetWidth();
        int frameHeight = videoFrame.GetHeight();
        videoFrame.GetAncillaryData(out IDeckLinkVideoFrameAncillary ancillaryFrameData);
        _BMDDisplayMode mode = ancillaryFrameData.GetDisplayMode();
        _BMDPixelFormat fmt = ancillaryFrameData.GetPixelFormat();

        IDeckLinkVideoFrameAncillaryPackets ancillaryData = (IDeckLinkVideoFrameAncillaryPackets)videoFrame;
        ancillaryData.GetPacketIterator(out IDeckLinkAncillaryPacketIterator it);

        it.Next(out IDeckLinkAncillaryPacket packet);
        while (packet != null)
        {
            byte did = packet.GetDID();
            byte sdid = packet.GetSDID();
            if (did == DIDPacketBlackmagic && sdid == SDIDTally)
            {
                //Tally packet, don't care...
            }
            else if (did == DIDPacketBlackmagic && sdid == SDIDCameraControl)
            {
                uint line = packet.GetLineNumber();

                packet.GetBytes(_BMDAncillaryPacketFormat.bmdAncillaryPacketFormatUInt8, out IntPtr packetData,
                    out uint size);
                unsafe
                {
                    using Stream ms = new UnmanagedMemoryStream((byte*)packetData.ToPointer(), size);
                    CommandReader.DecodeStream(ms,
                        (a_id, a_packet) => { m_controller.OnCameraPacketArrived(CameraHandle, a_id, a_packet); });
                }
            }
            else
            {
                throw new Exception($"Unexpected packet identifier DID: {did}, SDID: {sdid}");
            }

            it.Next(out packet);
        }
    }

    public void StartVideoInput()
    {
        m_targetDevice.EnableVideoInput(m_targetDisplayMode, m_targetPixelFormat,
            _BMDVideoInputFlags.bmdVideoInputEnableFormatDetection);
    }
}