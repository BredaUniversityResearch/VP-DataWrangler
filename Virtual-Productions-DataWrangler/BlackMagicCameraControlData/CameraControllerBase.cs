using BlackmagicCameraControl.CommandPackets;
using DataWranglerCommon;

namespace BlackmagicCameraControlData
{
	public class CameraControllerBase
	{
		public delegate void CameraConnectedDelegate(CameraDeviceHandle a_deviceHandle);
		public delegate void CameraDataReceivedDelegate(CameraDeviceHandle a_deviceHandle, TimeCode a_receivedTime, ICommandPacketBase a_packet);
		
        public event CameraConnectedDelegate OnCameraConnected = delegate { };
		public event CameraConnectedDelegate OnCameraDisconnected = delegate { };
		public event CameraDataReceivedDelegate OnCameraDataReceived = delegate { };

        protected void CameraConnected(CameraDeviceHandle a_deviceHandle)
		{
			OnCameraConnected(a_deviceHandle);
		}

		protected void CameraDisconnected(CameraDeviceHandle a_deviceHandle)
		{
			OnCameraDisconnected(a_deviceHandle);
		}

		protected void CameraDataReceived(CameraDeviceHandle a_deviceHandle, TimeCode a_receivedTime, ICommandPacketBase a_packet)
		{
			OnCameraDataReceived(a_deviceHandle, a_receivedTime, a_packet);
		}

		public bool TrySendAsyncCommand(CameraDeviceHandle a_deviceHandle, ICommandPacketBase a_command)
		{
			return false;
		}
	}	
}
