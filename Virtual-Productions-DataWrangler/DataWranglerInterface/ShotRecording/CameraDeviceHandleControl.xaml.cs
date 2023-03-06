using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BlackmagicCameraControlData;

namespace DataWranglerInterface.ShotRecording
{
	public partial class CameraDeviceHandleControl : UserControl
	{
		public class DragDropInfo
		{
			public readonly ActiveCameraInfo SourceCameraInfo;
			public readonly CameraDeviceHandle SourceDeviceHandle;

			public DragDropInfo(ActiveCameraInfo a_sourceCameraInfo, CameraDeviceHandle a_sourceDeviceHandle)
			{
				SourceCameraInfo = a_sourceCameraInfo;
				SourceDeviceHandle = a_sourceDeviceHandle;
			}
		};

		public readonly CameraDeviceHandle DeviceHandle;
		private ActiveCameraInfo m_parentCameraInfo;
		public string ConnectionDeviceUuid => DeviceHandle.DeviceUuid;

		public CameraDeviceHandleControl(ActiveCameraInfo a_cameraInfo, CameraDeviceHandle a_handle)
		{
			m_parentCameraInfo = a_cameraInfo;
			DeviceHandle = a_handle;
			InitializeComponent();
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			if (e.LeftButton == MouseButtonState.Pressed)
			{
				DragDrop.DoDragDrop(this, new DragDropInfo(m_parentCameraInfo, DeviceHandle), DragDropEffects.Copy | DragDropEffects.Move);
			}
		}
	}
}
