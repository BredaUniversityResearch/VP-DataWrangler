using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;
using BlackmagicCameraControlBluetooth;
using BlackmagicCameraControlData;
using BlackmagicCameraControlData.CommandPackets;
using BlackmagicDeckLinkControl;
using CameraControlOverEthernet;
using CommonLogging;
using DataWranglerCommon.CameraHandling;
using DataWranglerInterface.Configuration;
using DataWranglerInterface.ShotRecording;

namespace DataWranglerInterface.CameraHandling
{
    public class ActiveCameraHandler
    {
        private BlackmagicBluetoothCameraAPIController? m_bluetoothController = null;
        private BlackmagicDeckLinkController? m_deckLinkController = null;
        private EthernetRelayCameraController m_relayCameraControl = new EthernetRelayCameraController();
        private List<ActiveCameraInfo> m_activeCameras = new List<ActiveCameraInfo>();

        public delegate void CameraConnectedHandler(ActiveCameraInfo a_camera);
        public delegate void CameraDisconnectedHandler(ActiveCameraInfo a_handle);

        public event CameraConnectedHandler OnVirtualCameraConnected = delegate { };
        public event CameraDisconnectedHandler OnCameraDisconnected = delegate { };

        public VideoPreviewControl? PreviewControl { get; set; }
        private Task? PreviewUpdateTask = null;

        private CancellationTokenSource m_backgroundTaskCancellationSource = new CancellationTokenSource();
        private Task m_backgroundDispatchTask;

        public ActiveCameraHandler(BlackmagicBluetoothCameraAPIController? a_bluetoothController)
        {
            m_bluetoothController = a_bluetoothController;
            if (m_bluetoothController != null)
            {
	            SubscribeCameraController(m_bluetoothController);
            }

            SubscribeCameraController(m_relayCameraControl);

			m_deckLinkController = BlackmagicDeckLinkController.Create(out string? errorMessage);
            if (m_deckLinkController != null)
            {
	            SubscribeCameraController(m_deckLinkController);
                m_deckLinkController.OnCameraFrameDataReceived += OnCameraFrameDataReceived;
            }
            else
            {
                Logger.LogWarning("ACH", $"Failed to create DeckLink controller. Reason: {errorMessage}");
            }

            if (m_deckLinkController != null)
            {
                PreviewUpdateTask = Task.Run(BackgroundUpdateFramePreview);
            }

            m_backgroundDispatchTask = Task.Run(BackgroundDispatchReceivedEvents);
        }

        private void BackgroundDispatchReceivedEvents()
        {
	        while (!m_backgroundTaskCancellationSource.IsCancellationRequested)
	        {
		        m_relayCameraControl.BlockingProcessReceivedMessages(TimeSpan.FromSeconds(10), m_backgroundTaskCancellationSource.Token);
	        }
        }

        private void SubscribeCameraController(CameraControllerBase a_cameraController)
        {
	        a_cameraController.OnCameraConnected += OnCommonCameraConnected;
	        a_cameraController.OnCameraDataReceived += OnCameraDataReceived;
	        a_cameraController.OnCameraDisconnected += OnCommonCameraDisconnected;
		}

        private void OnCameraDataReceived(CameraDeviceHandle a_deviceHandle, TimeCode a_receivedTime, ICommandPacketBase a_packet)
        {
            if (FindCameraInfoForDevice(a_deviceHandle, out ActiveCameraInfo? targetCamera))
            {
                targetCamera.OnCameraDataReceived(a_deviceHandle, a_receivedTime, a_packet);
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

        private bool FindCameraInfoForGrouping(ConfigActiveCameraGrouping a_grouping, [NotNullWhen(true)] out ActiveCameraInfo? a_cameraInfo)
        {
	        foreach (ActiveCameraInfo info in m_activeCameras)
	        {
		        if (info.Grouping == a_grouping)
		        {
			        a_cameraInfo = info;
			        return true;
		        }
	        }

	        a_cameraInfo = null;
	        return false;
        }

		private void OnCommonCameraConnected(CameraDeviceHandle a_deviceHandle)
		{
            ConfigActiveCameraGrouping? grouping = DataWranglerConfig.Instance.ConfiguredCameraGroupings.Find(a_obj => a_obj.DeviceHandleUuids.Contains(a_deviceHandle.DeviceUuid));
            ActiveCameraInfo? info = null;
            if (grouping != null)
            {
	            FindCameraInfoForGrouping(grouping, out info);
            }

            if (info == null)
            {
	            info = new ActiveCameraInfo(grouping);
				m_activeCameras.Add(info);
				info.CameraName = a_deviceHandle.DeviceUuid;
	            info.OnDeviceConnectionsChanged += OnCameraConnectionChanged;
				OnVirtualCameraConnected(info);
            }

            info.TransferCameraHandle(null, a_deviceHandle);
        }

        private void OnCameraConnectionChanged(ActiveCameraInfo a_source)
        {
            if (a_source.ConnectionsForPhysicalDevice.Count == 0)
            {
                OnCameraDisconnected(a_source);
                m_activeCameras.Remove(a_source);
            }

            //Update groupings
            DataWranglerConfig config = DataWranglerConfig.Instance;
            config.ConfiguredCameraGroupings.Clear();
            foreach (ActiveCameraInfo info in m_activeCameras)
            {
	            if (info.Grouping != null)
	            {
                    config.ConfiguredCameraGroupings.Add(info.Grouping);
	            }
            }
            config.Save();
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

        private void OnCameraFrameDataReceived(CameraDeviceHandle a_deviceHandle, int a_frameWidth, int a_frameHeight, IntPtr a_framePixelData, int a_stride)
        {
            if (PreviewControl != null)
            {
                PreviewControl.OnVideoFrameUpdated(a_frameWidth, a_frameHeight, PixelFormats.Bgra32, a_framePixelData, a_stride * a_frameHeight, a_stride);
            }
        }

        private void BackgroundUpdateFramePreview()
        {
	        while (m_deckLinkController != null)
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
        }
	}
}
