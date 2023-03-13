using System.Diagnostics.CodeAnalysis;
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
		private List<ActiveCameraInfo> m_activeCameras = new List<ActiveCameraInfo>();

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
			m_bluetoothController.OnCameraDisconnected += OnCommonCameraDisconnected;

			m_deckLinkController = BlackmagicDeckLinkController.Create(out string? errorMessage);
			if (m_deckLinkController != null)
			{
				m_deckLinkController.OnCameraConnected += OnCommonCameraConnected;
				m_deckLinkController.OnCameraDataReceived += OnCameraDataReceived;
				m_deckLinkController.OnCameraDisconnected += OnCommonCameraDisconnected;
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

        private void OnCameraDataReceived(CameraDeviceHandle a_deviceHandle, TimeCode a_receivedTime, ICommandPacketBase a_packet)
		{
			if (FindCameraInfoForDevice(a_deviceHandle, out ActiveCameraInfo? targetCamera))
			{
				targetCamera.OnCameraDataReceived(m_bluetoothController, a_deviceHandle, a_receivedTime, a_packet);
			}
			else
			{
				Logger.LogWarning("ACH",
					$"Received data for camera {a_deviceHandle.DeviceUuid} but camera is not actively connected");
			}
		}

        private bool FindCameraInfoForDevice(CameraDeviceHandle a_handle, [NotNullWhen(true)] out ActiveCameraInfo? a_cameraInfo)
        {
	        foreach (ActiveCameraInfo cameraInfo in m_activeCameras)
	        {
		        if (cameraInfo.ConnectionsForPhysicalDevice.Contains(a_handle))
		        {
			        a_cameraInfo = cameraInfo;
			        return true;
		        }
	        }

	        a_cameraInfo = null;
	        return false;
        }

        private void OnBluetoothCameraConnected(CameraDeviceHandle a_deviceHandle)
		{
			if (m_bluetoothController == null)
			{
				throw new Exception();
			}

			OnCommonCameraConnected(a_deviceHandle);
			m_bluetoothController.AsyncRequestCameraModel(a_deviceHandle);
		}

		private void OnCommonCameraConnected(CameraDeviceHandle a_deviceHandle)
		{
			ActiveCameraInfo info = new ActiveCameraInfo(a_deviceHandle);
			info.CameraName = a_deviceHandle.DeviceUuid;
			info.DeviceConnectionsChanged += OnCameraConnectionChanged;
			m_activeCameras.Add(info);
			OnCameraConnected(info);
		}

		private void OnCameraConnectionChanged(ActiveCameraInfo a_source)
		{
			if (a_source.ConnectionsForPhysicalDevice.Count == 0)
			{
				OnCameraDisconnected(a_source);
				m_activeCameras.Remove(a_source);
			}
		}

		private void OnCommonCameraDisconnected(CameraDeviceHandle a_deviceHandle)
		{
			if (FindCameraInfoForDevice(a_deviceHandle, out ActiveCameraInfo? info))
			{
				OnCameraDisconnected(info);
				m_activeCameras.Remove(info);
			}
			else
			{
				Logger.LogWarning("ACH", "Got device disconnect event, but no virtual camera was found using this handle");
			}
		}
		
        private void OnCameraFrameDataReceived(CameraDeviceHandle a_deviceHandle, int a_framewidth, int a_frameheight, IntPtr a_framepixeldata, int a_stride)
        {
            if (PreviewControl != null)
            {
				PreviewControl.OnVideoFrameUpdated(a_framewidth, a_frameheight, PixelFormats.Bgra32, a_framepixeldata, a_stride * a_frameheight, a_stride);
            }
        }
    }
}
