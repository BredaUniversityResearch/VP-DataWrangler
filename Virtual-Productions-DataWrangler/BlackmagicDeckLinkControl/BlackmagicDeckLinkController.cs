using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using BlackmagicCameraControl.CommandPackets;
using BlackmagicCameraControlData;
using BlackmagicCameraControlData.CommandPackets;
using DataWranglerCommon;
using DeckLinkAPI;

namespace BlackmagicDeckLinkControl
{
	public class BlackmagicDeckLinkController : CameraControllerBase
	{
		private const int FrameQueueLength = 2;

		private DeckLinkDeviceNotificationHandler m_deckLinkDeviceNotificationHandler;
		private IDeckLinkDiscovery m_deckLinkDiscovery = new CDeckLinkDiscoveryClass();

		private Dictionary<CameraDeviceHandle, CameraPropertyCache> m_activeCameras =
			new Dictionary<CameraDeviceHandle, CameraPropertyCache>();

		public delegate void CameraFrameDataReceivedDelegate(CameraDeviceHandle a_deviceHandle, int a_frameWidth, int a_frameHeight,
			IntPtr a_framePixelData, int a_stride);

		public event CameraFrameDataReceivedDelegate OnCameraFrameDataReceived = delegate { };

		public readonly ConcurrentQueue<DeckLinkVideoConversionFrame> FrameQueue = new ConcurrentQueue<DeckLinkVideoConversionFrame>();


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

		public void OnDeckLinkDeviceArrived(CameraDeviceHandle a_deviceHandle)
		{
			m_activeCameras.Add(a_deviceHandle, new CameraPropertyCache());
			CameraConnected(a_deviceHandle);
		}

		public void OnCameraDeviceRemoved(CameraDeviceHandle a_deviceHandle)
		{
			throw new NotImplementedException();
		}

		public void OnCameraPacketArrived(CameraDeviceHandle a_deviceHandle, CommandIdentifier a_id, ICommandPacketBase a_packet, TimeCode a_timeCode)
		{
			if (m_activeCameras.TryGetValue(a_deviceHandle, out CameraPropertyCache? cache))
			{
				if (cache.CheckPropertyChanged(a_id, a_packet))
				{
					if (!(a_id.Category == 9 && a_id.Parameter == 4) && //Ignore reference time packet for now.
						!(a_id.Category == 9 && a_id.Parameter == 0)) //And the battery info packets...
					{
						BlackmagicCameraLogInterface.LogInfo($"Received Packet {a_id.Category}:{a_id.Parameter}. {a_packet}");
					}
                    CameraDataReceived(a_deviceHandle, a_timeCode, a_packet);
				}
			}
		}

		public void OnVideoFrameReceived(IDeckLinkVideoFrame a_videoFrame)
		{
			int frameWidth = a_videoFrame.GetWidth();
			int frameHeight = a_videoFrame.GetHeight();
			_BMDPixelFormat pixelFormat = a_videoFrame.GetPixelFormat();
			IntPtr framePixelData;
			a_videoFrame.GetBytes(out framePixelData);

			if (pixelFormat != _BMDPixelFormat.bmdFormat8BitARGB)
			{
				var conversionFrame = new DeckLinkVideoConversionFrame(frameWidth, frameHeight, _BMDPixelFormat.bmdFormat8BitBGRA);

				IDeckLinkVideoConversion converter = new CDeckLinkVideoConversionClass();
				converter.ConvertFrame(a_videoFrame, conversionFrame);

				FrameQueue.Enqueue(conversionFrame);
				while (FrameQueue.Count > FrameQueueLength)
				{
					if (FrameQueue.TryDequeue(out var discardedFrame))
					{
						discardedFrame.Dispose();
					}
				}
			}
			else
			{
				throw new Exception(
					"Invalid pixel format. Does not need conversion. This should be fine to pass through but never tested.");
			}
		}
	}
}