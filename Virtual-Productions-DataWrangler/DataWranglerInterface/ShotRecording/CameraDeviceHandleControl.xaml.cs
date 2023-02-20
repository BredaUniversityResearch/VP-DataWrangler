using System.Security.RightsManagement;
using System.Windows.Controls;
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
	}
}
