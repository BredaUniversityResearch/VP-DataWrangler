using BlackmagicCameraControl;
using BlackmagicCameraControl.CommandPackets;
using DataWranglerInterface.DebugSupport;

namespace DataWranglerInterface.ShotRecording
{
	public class ActiveCameraHandler
	{
		private BlackmagicCameraAPIController m_apiController;
		private Dictionary<CameraHandle, ActiveCameraInfo> m_activeCameras = new Dictionary<CameraHandle, ActiveCameraInfo>();

		public delegate void CameraConnectedHandler(ActiveCameraInfo a_camera);
		public delegate void CameraDisconnectedHandler(ActiveCameraInfo a_handle);

		public event CameraConnectedHandler OnCameraConnected = delegate { };
		public event CameraDisconnectedHandler OnCameraDisconnected = delegate { };

		public ActiveCameraHandler(BlackmagicCameraAPIController a_apiController)
		{
			m_apiController = a_apiController;
			m_apiController.OnCameraConnected += OnApiControllerCameraConnected;
			m_apiController.OnCameraDataReceived += OnCameraDataReceived;
			m_apiController.OnCameraDisconnected += OnApiControllerCameraDisconnected;
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

		private void OnApiControllerCameraConnected(CameraHandle a_handle)
		{
			if (m_apiController == null)
			{
				throw new Exception();
			}

			ActiveCameraInfo info = new ActiveCameraInfo(a_handle);
			m_apiController.AsyncRequestCameraModel(a_handle);
			m_activeCameras.Add(a_handle, info);
			OnCameraConnected(info);

			//string cameraDisplayName = m_apiController.GetBluetoothName(a_handle);
			//Dispatcher.InvokeAsync(() =>
			//	{
			//		LoadingSpinner.Visibility = Visibility.Hidden;
			//		CameraDisplayName.Content = cameraDisplayName;
			//	}
			//);

			//m_controller.AsyncRequestCameraName(a_handle);
		}

		private void OnApiControllerCameraDisconnected(CameraHandle a_handle)
		{
			if (m_activeCameras.TryGetValue(a_handle, out ActiveCameraInfo? info))
			{
				OnCameraDisconnected(info);
				m_activeCameras.Remove(a_handle);
			}
		}
	}
}
