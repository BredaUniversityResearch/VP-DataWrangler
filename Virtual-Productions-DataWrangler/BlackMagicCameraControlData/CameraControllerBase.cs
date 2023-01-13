using BlackmagicCameraControl.CommandPackets;

namespace BlackmagicCameraControlData
{
	public class CameraControllerBase
	{
		public delegate void CameraConnectedDelegate(CameraHandle a_handle);
		public delegate void CameraDataReceivedDelegate(CameraHandle a_handle, DateTimeOffset a_receivedTime, ICommandPacketBase a_packet);
		
        public event CameraConnectedDelegate OnCameraConnected = delegate { };
		public event CameraConnectedDelegate OnCameraDisconnected = delegate { };
		public event CameraDataReceivedDelegate OnCameraDataReceived = delegate { };


        protected void CameraConnected(CameraHandle a_handle)
		{
			OnCameraConnected(a_handle);
		}

		protected void CameraDisconnected(CameraHandle a_handle)
		{
			OnCameraDisconnected(a_handle);
		}

		protected void CameraDataReceived(CameraHandle a_handle, DateTimeOffset a_receivedTime, ICommandPacketBase a_packet)
		{
			OnCameraDataReceived(a_handle, a_receivedTime, a_packet);
		}
    }
}
