﻿using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;
using BlackmagicCameraControlData;
using BlackmagicCameraControlData.CommandPackets;
using BlackmagicDeckLinkControl;
using CameraControlOverEthernet.CameraControl;
using CommonLogging;
using DataWranglerCommon.CameraHandling;
using DataWranglerInterface.Configuration;
using DataWranglerInterface.ShotRecording;

namespace DataWranglerInterface.CameraHandling
{
    public class ActiveCameraHandler
    {
        private BlackmagicDeckLinkController? m_deckLinkController = null;
        private List<ActiveCameraInfo> m_activeCameras = new List<ActiveCameraInfo>();

        public delegate void CameraConnectedHandler(ActiveCameraInfo a_camera);
        public delegate void CameraDisconnectedHandler(ActiveCameraInfo a_handle);

        public event CameraConnectedHandler OnVirtualCameraConnected = delegate { };
        public event CameraDisconnectedHandler OnCameraDisconnected = delegate { };

        public VideoPreviewControl? PreviewControl { get; set; }
        private Task? PreviewUpdateTask = null;

        public ActiveCameraHandler(EthernetRelayCameraController? a_relayCameraController)
        {
            if (a_relayCameraController != null)
            {
				SubscribeCameraController(a_relayCameraController);
            }

            m_deckLinkController = BlackmagicDeckLinkController.TryCreate(out string? errorMessage);
            if (m_deckLinkController != null)
            {
	            SubscribeCameraController(m_deckLinkController);
                m_deckLinkController.OnCameraFrameDataReceived += OnCameraFrameDataReceived;
            }
            else
            {
                Logger.LogInfo("ACH", $"Failed to create DeckLink controller. Reason: {errorMessage}");
            }

            if (m_deckLinkController != null)
            {
                PreviewUpdateTask = Task.Run(BackgroundUpdateFramePreview);
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
                    $"Received data for camera {a_deviceHandle.DeviceUuid} but camera is not actively connected. Packet: {a_packet.GetType()}");
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
            ConfigActiveCameraGrouping? grouping = DataWranglerInterfaceConfig.Instance.ConfiguredCameraGroupings.Find(a_obj => a_obj.DeviceHandleUuids.Contains(a_deviceHandle.DeviceUuid));
            ActiveCameraInfo? info = null;
            if (grouping != null)
            {
	            FindCameraInfoForGrouping(grouping, out info);
            }

            if (info == null)
            {
	            info = new ActiveCameraInfo(grouping);
				m_activeCameras.Add(info);
				if (string.IsNullOrEmpty(info.CameraName))
				{
					info.CameraName = a_deviceHandle.DeviceUuid;
				}

				info.OnDeviceConnectionsChanged += OnCameraConnectionChanged;
	            info.PropertyChanged += OnCameraPropertyChanged;
				OnVirtualCameraConnected(info);
            }

            if (!info.ContainsHandle(a_deviceHandle))
            {
	            info.TransferCameraHandle(null, a_deviceHandle);
            }
		}

		private void OnCameraConnectionChanged(ActiveCameraInfo a_source)
		{
			if (a_source.ConnectionsForPhysicalDevice.Count == 0)
            {
                OnCameraDisconnected(a_source);
                m_activeCameras.Remove(a_source);
            }

            SaveCameraGroupings();
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

        private void OnCameraPropertyChanged(object? a_sender, PropertyChangedEventArgs a_e)
        {
	        if (a_e.PropertyName == nameof(ActiveCameraInfo.CameraName))
	        {
				SaveCameraGroupings();
	        }
        }

        private void SaveCameraGroupings()
        {
	        DataWranglerInterfaceConfig interfaceConfig = DataWranglerInterfaceConfig.Instance;
	        interfaceConfig.ConfiguredCameraGroupings.Clear();
	        foreach (ActiveCameraInfo info in m_activeCameras)
	        {
                if (info.Grouping == null && info.ConnectionsForPhysicalDevice.Count > 0)
		        {
			        info.Grouping = new ConfigActiveCameraGrouping();
			        foreach (CameraDeviceHandle handle in info.ConnectionsForPhysicalDevice)
			        {
				        info.Grouping.DeviceHandleUuids.Add(handle.DeviceUuid);
			        }
		        }

                if (info.Grouping != null)
                {
	                interfaceConfig.ConfiguredCameraGroupings.Add(info.Grouping);
                }
	        }

	        interfaceConfig.MarkDirty();
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
