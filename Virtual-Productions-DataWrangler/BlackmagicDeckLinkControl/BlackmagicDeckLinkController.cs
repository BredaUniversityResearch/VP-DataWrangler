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

		private Dictionary<CameraHandle, CameraPropertyCache> m_activeCameras =
			new Dictionary<CameraHandle, CameraPropertyCache>();

		public delegate void CameraFrameDataReceivedDelegate(CameraHandle a_handle, int a_frameWidth, int a_frameHeight,
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

		public void OnDeckLinkDeviceArrived(CameraHandle a_handle)
		{
			m_activeCameras.Add(a_handle, new CameraPropertyCache());
			CameraConnected(a_handle);
		}

		public void OnCameraDeviceRemoved(CameraHandle a_handle)
		{
			throw new NotImplementedException();
		}

		public void OnCameraPacketArrived(CameraHandle a_handle, CommandIdentifier a_id, ICommandPacketBase a_packet, TimeCode a_timeCode)
		{
			Debugger.Break(); // Need to verify timecode.

			if (m_activeCameras.TryGetValue(a_handle, out CameraPropertyCache? cache))
			{
				if (cache.CheckPropertyChanged(a_id, a_packet))
				{
					if (!(a_id.Category == 9 && a_id.Parameter == 4) && //Ignore reference time packet for now.
						!(a_id.Category == 9 && a_id.Parameter == 0)) //And the battery info packets...
					{
						BlackmagicCameraLogInterface.LogInfo($"Received Packet {a_id.Category}:{a_id.Parameter}. {a_packet}");
					}
                    CameraDataReceived(a_handle, DateTimeOffset.UtcNow, a_packet);
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