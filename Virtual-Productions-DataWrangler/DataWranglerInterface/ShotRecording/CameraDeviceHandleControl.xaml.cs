using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BlackmagicCameraControlData;

namespace DataWranglerInterface.ShotRecording
{
	public partial class CameraDeviceHandleControl : UserControl
	{
		private CameraDeviceHandle m_deviceHandle;
		public string ConnectionDeviceUuid => m_deviceHandle.DeviceUuid;

		public CameraDeviceHandleControl(CameraDeviceHandle a_handle)
		{
			m_deviceHandle = a_handle;
			InitializeComponent();
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			if (e.LeftButton == MouseButtonState.Pressed)
			{
				DataObject data = new DataObject();
				data.SetData("SourceObject", this);
				DragDrop.DoDragDrop(this, data, DragDropEffects.Copy | DragDropEffects.Move);
			}
		}
	}
}
