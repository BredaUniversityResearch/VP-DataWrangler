using System;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using BlackmagicCameraControl;
using BlackmagicCameraControl.CommandPackets;
using DeckLinkAPI;
using IntPtr = System.IntPtr;

namespace BlackmagicDeckLinkControl
{
	public class BlackmagicDeckLinkController
	{
		//http://www.watersprings.org/pub/id/draft-ietf-payload-rtp-ancillary-13.html
		[StructLayout(LayoutKind.Explicit)]
		struct SMTPE_291M_Packet
		{
			[FieldOffset(0)]
			public uint zero;

			[FieldOffset(4)]
			public uint magic;

			[FieldOffset(8)]
			public uint DIDSDIDFieldLength;
		};

		class DeviceInputNotificationCallback : IDeckLinkInputCallback
		{
			private const int RemoteControlVANCLineId = 16; // Ursa Mini Manual SDK 1.4, pg 271 "Blanking Encoding"
			private const int MaxVANCLineByteSize = 255;

			private IDeckLinkInput m_targetDevice;
			private _BMDDisplayMode m_targetDisplayMode = _BMDDisplayMode.bmdModeHD1080p6000;
			private _BMDPixelFormat m_targetPixelFormat = _BMDPixelFormat.bmdFormat10BitYUV;

			public DeviceInputNotificationCallback(IDeckLinkInput a_targetDevice)
			{
				m_targetDevice = a_targetDevice;
			}

			public void VideoInputFormatChanged(_BMDVideoInputFormatChangedEvents notificationEvents, IDeckLinkDisplayMode newDisplayMode, _BMDDetectedVideoInputFormatFlags detectedSignalFlags)
			{
				_BMDDisplayMode targetMode = m_targetDisplayMode;
				_BMDPixelFormat pixelFormat = m_targetPixelFormat;

				if ((notificationEvents & _BMDVideoInputFormatChangedEvents.bmdVideoInputDisplayModeChanged) != 0)
				{
					targetMode = newDisplayMode.GetDisplayMode();
				}

				if ((notificationEvents & _BMDVideoInputFormatChangedEvents.bmdVideoInputColorspaceChanged) != 0)
				{
					//switch(detectedSignalFlags)
					//{
					//	case _BMDDetectedVideoInputFormatFlags.bmdDetectedVideoInput8BitDepth:
					//                       pixelFormat = _BMDPixelFormat.bmdFormat8BitARGB;
					//                       break;
					//	case _BMDDetectedVideoInputFormatFlags.bmdDetectedVideoInput10BitDepth:
					//                       pixelFormat = _BMDPixelFormat.bmdFormat10BitRGB;
					//                       break;
					//	case _BMDDetectedVideoInputFormatFlags.bmdDetectedVideoInput12BitDepth:
					//                       pixelFormat = _BMDPixelFormat.bmdFormat12BitRGB;
					//                       break;
					//	default:
					//                       pixelFormat = m_targetPixelFormat;
					//                       break;
					//               };
					pixelFormat = _BMDPixelFormat.bmdFormat10BitYUV;
				}

				if (m_targetDisplayMode != targetMode || m_targetPixelFormat != pixelFormat)
				{
					m_targetDisplayMode = targetMode;
					m_targetPixelFormat = pixelFormat;

					m_targetDevice.PauseStreams();
					m_targetDevice.EnableVideoInput(targetMode, pixelFormat, _BMDVideoInputFlags.bmdVideoInputEnableFormatDetection);
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
				//videoFrame.GetAncillaryData(out IDeckLinkVideoFrameAncillary ancillaryData);
				//_BMDDisplayMode mode = ancillaryData.GetDisplayMode();

				IDeckLinkVideoFrameAncillaryPackets ancillaryData = (IDeckLinkVideoFrameAncillaryPackets) videoFrame;
				ancillaryData.GetPacketIterator(out IDeckLinkAncillaryPacketIterator it);

				it.Next(out IDeckLinkAncillaryPacket packet);
				while (packet != null)
				{
					byte did = packet.GetDID(); //0x51
					byte sdid = packet.GetSDID(); //0x53
					if (did == 0x51 && sdid == 0x52)
					{
						//Tally packet, don't care...
					}
					else if (did == 0x51 && sdid == 0x53)
					{
						uint line = packet.GetLineNumber();

						packet.GetBytes(_BMDAncillaryPacketFormat.bmdAncillaryPacketFormatUInt8, out IntPtr packetData, out uint size);
						unsafe
						{
							using Stream ms = new UnmanagedMemoryStream((byte*) packetData.ToPointer(), size);
							byte[] data = new byte[size];
							ms.Read(data, 0, (int)size);
							ms.Seek(0, SeekOrigin.Begin);
							CommandReader.DecodeStream(ms, (a_packet) =>
							{
								int b = 9001;
							});
						}
					}
					else
					{
						throw new Exception($"Unexpected packet identifier DID: {did}, SDID: {sdid}");
					}

					it.Next(out packet);
				}


				int a = 9;
				//try
				//{
				//	IntPtr buffer;
				//	ancillaryData.GetBufferForVerticalBlankingLine(RemoteControlVANCLineId, out buffer);

				//	unsafe
				//	{
				//		using Stream headerStream = new UnmanagedMemoryStream((byte*)buffer.ToPointer(), MaxVANCLineByteSize);
				//		byte[] SMPTEHeader = new byte[64];
				//		int read = headerStream.Read(SMPTEHeader);

				//		CommandReader reader = new CommandReader(headerStream);

				//		PacketHeader header = reader.ReadPacketHeader();

				//		reader.DecodeCommandStream(header, (a_packet) =>
				//		{
				//			int a = 9;
				//			//IBlackmagicCameraLogInterface.LogInfo()
				//		});
				//	}
				//}
				//catch (COMException)
				//{
				//                //This can fail when there's no data? 
				//                //https://forum.blackmagicdesign.com/viewtopic.php?f=3&t=23142
				//            }
			}
		}

		class DeviceNotificationCallback : IDeckLinkDeviceNotificationCallback
		{
			private DeviceInputNotificationCallback? m_deviceInputNotificationCallback = null;

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

					m_deviceInputNotificationCallback = new DeviceInputNotificationCallback(input);
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