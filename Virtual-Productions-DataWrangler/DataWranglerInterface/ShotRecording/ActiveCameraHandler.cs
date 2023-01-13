using System.Windows.Media;
using BlackmagicCameraControl;
using BlackmagicCameraControl.CommandPackets;
using BlackmagicCameraControlBluetooth;
using BlackmagicCameraControlData;
using BlackmagicDeckLinkControl;
using CommonLogging;
using DataWranglerCommon;

namespace DataWranglerInterface.ShotRecording
{
	public class ActiveCameraHandler
	{
		private BlackmagicBluetoothCameraAPIController m_bluetoothController;
		private BlackmagicDeckLinkController? m_deckLinkController = null;
		private Dictionary<CameraHandle, ActiveCameraInfo> m_activeCameras = new Dictionary<CameraHandle, ActiveCameraInfo>();

		public delegate void CameraConnectedHandler(ActiveCameraInfo a_camera);
		public delegate void CameraDisconnectedHandler(ActiveCameraInfo a_handle);

		public event CameraConnectedHandler OnCameraConnected = delegate { };
        public event CameraDisconnectedHandler OnCameraDisconnected = delegate { };

        public VideoPreviewControl? PreviewControl { get; set; }
        private Task? PreviewUpdateTask = null;

        public ActiveCameraHandler(BlackmagicBluetoothCameraAPIController a_bluetoothController)
		{
			m_bluetoothController = a_bluetoothController;
			m_bluetoothController.OnCameraConnected += OnBluetoothCameraConnected;
			m_bluetoothController.OnCameraDataReceived += OnCameraDataReceived;
			m_bluetoothController.OnCameraDisconnected += OnBluetoothCameraDisconnected;

			m_deckLinkController = BlackmagicDeckLinkController.Create(out string? errorMessage);
			if (m_deckLinkController != null)
			{
				m_deckLinkController.OnCameraConnected += OnBluetoothCameraConnected;
				m_deckLinkController.OnCameraDataReceived += OnCameraDataReceived;
				m_deckLinkController.OnCameraDisconnected += OnBluetoothCameraDisconnected;
				m_deckLinkController.OnCameraFrameDataReceived += OnCameraFrameDataReceived;
			}
			else
			{
				Logger.LogWarning("ACH", $"Failed to create DeckLink controller. Reason: {errorMessage}");
			}

			if (m_deckLinkController != null)
			{
				PreviewUpdateTask = Task.Run(() =>
				{
					while (true)
					{
						if (m_deckLinkController.FrameQueue.TryDequeue(out var frame))
						{
							if (PreviewControl != null)
							{
								PreviewControl.Dispatcher.InvokeAsync(() =>
								{
									frame.GetBytes(out IntPtr pixelBuffer);
									PreviewControl.OnVideoFrameUpdated(frame.GetWidth(), frame.GetHeight(),
										PixelFormats.Bgra32, pixelBuffer, frame.GetRowBytes() * frame.GetHeight(),
										frame.GetRowBytes());
									frame.Dispose();
								});
							}
						}
					}
				});
			}
		}

        private void OnCameraDataReceived(CameraHandle a_handle, DateTimeOffset a_receivedTime, ICommandPacketBase a_packet)
		{
			if (m_activeCameras.TryGetValue(a_handle, out ActiveCameraInfo? targetCamera))
			{
				targetCamera.OnCameraDataReceived(m_bluetoothController, a_receivedTime, a_packet);
			}
			else
			{
				Logger.LogWarning("ACH",
					$"Received data for camera {a_handle.ConnectionId} but camera is not actively connected");
			}
		}

		private void OnBluetoothCameraConnected(CameraHandle a_handle)
		{
			if (m_bluetoothController == null)
			{
				throw new Exception();
			}

			ActiveCameraInfo info = new ActiveCameraInfo(a_handle);
			m_bluetoothController.AsyncRequestCameraModel(a_handle);
			m_activeCameras.Add(a_handle, info);
			OnCameraConnected(info);
		}

		private void OnBluetoothCameraDisconnected(CameraHandle a_handle)
		{
			if (m_activeCameras.TryGetValue(a_handle, out ActiveCameraInfo? info))
			{
				OnCameraDisconnected(info);
				m_activeCameras.Remove(a_handle);
			}
		}
		
        private void OnCameraFrameDataReceived(CameraHandle a_handle, int a_framewidth, int a_frameheight, IntPtr a_framepixeldata, int a_stride)
        {
            if (PreviewControl != null)
            {
				PreviewControl.OnVideoFrameUpdated(a_framewidth, a_frameheight, PixelFormats.Bgra32, a_framepixeldata, a_stride * a_frameheight, a_stride);
            }
        }
    }
}
