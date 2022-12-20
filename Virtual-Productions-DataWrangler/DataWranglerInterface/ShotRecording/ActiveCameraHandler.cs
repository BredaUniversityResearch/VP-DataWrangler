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
		private BlackmagicBluetoothCameraAPIController m_apiController;
		private BlackmagicDeckLinkController? m_deckLinkController = null;
		private Dictionary<CameraHandle, ActiveCameraInfo> m_activeCameras = new Dictionary<CameraHandle, ActiveCameraInfo>();

		public delegate void CameraConnectedHandler(ActiveCameraInfo a_camera);
		public delegate void CameraDisconnectedHandler(ActiveCameraInfo a_handle);

		public event CameraConnectedHandler OnCameraConnected = delegate { };
		public event CameraDisconnectedHandler OnCameraDisconnected = delegate { };

		public ActiveCameraHandler(BlackmagicBluetoothCameraAPIController a_apiController)
		{
			m_apiController = a_apiController;
			m_apiController.OnCameraConnected += OnBluetoothCameraConnected;
			m_apiController.OnCameraDataReceived += OnCameraDataReceived;
			m_apiController.OnCameraDisconnected += OnBluetoothCameraDisconnected;

			m_deckLinkController = BlackmagicDeckLinkController.Create(out string? errorMessage);
			if (m_deckLinkController != null)
			{
			}
			else
			{
				Logger.LogWarning("ACH", $"Failed to create DeckLink controller. Reason: {errorMessage}");
			}
		}

		private void OnCameraDataReceived(CameraHandle a_handle, DateTimeOffset a_receivedTime, ICommandPacketBase a_packet)
		{
			if (m_activeCameras.TryGetValue(a_handle, out ActiveCameraInfo? targetCamera))
			{
				targetCamera.OnCameraDataReceived(m_apiController, a_receivedTime, a_packet);
			}
			else
			{
				Logger.LogWarning("ACH",
					$"Received data for camera {a_handle.ConnectionId} but camera is not actively connected");
			}
		}

		private void OnBluetoothCameraConnected(CameraHandle a_handle)
		{
			if (m_apiController == null)
			{
				throw new Exception();
			}

			ActiveCameraInfo info = new ActiveCameraInfo(a_handle);
			m_apiController.AsyncRequestCameraModel(a_handle);
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
	}
}
