using System.Windows;
using System.Windows.Controls;
using BlackmagicCameraControl;
using BlackmagicCameraControl.CommandPackets;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for CameraInfoDebugControl.xaml
	/// </summary>
	public partial class CameraInfoDebugControl : UserControl
	{
		public BlackmagicBluetoothCameraAPIController? CameraApiController = null;
		private ActiveCameraInfo? m_activeCamera = null;

		public CameraInfoDebugControl()
		{
			InitializeComponent();
		}

		public void SetTargetCamera(ActiveCameraInfo? a_activeCamera)
		{
			m_activeCamera = a_activeCamera;
		}

		private void OnClickDebugConnectCamera(object a_sender, RoutedEventArgs a_e)
		{
			if (CameraApiController != null)
			{
				CameraApiController.CreateDebugCameraConnection();
			}
		}

		private void OnClickDebugToggleRecording(object a_sender, RoutedEventArgs a_e)
		{
			if (m_activeCamera != null && CameraApiController != null)
			{
				CommandPacketMediaTransportMode.EMode currentMode = m_activeCamera.CurrentTransportMode;
				CameraApiController.AsyncSendCommand(m_activeCamera.TargetCamera, new CommandPacketMediaTransportMode {
					Mode = currentMode == CommandPacketMediaTransportMode.EMode.Record? CommandPacketMediaTransportMode.EMode.Preview : CommandPacketMediaTransportMode.EMode.Record
				});
			}
		}
	}
}
