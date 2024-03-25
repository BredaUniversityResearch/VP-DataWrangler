using System.Windows;
using System.Windows.Controls;
using BlackmagicCameraControlData;
using BlackmagicCameraControlData.CommandPackets;
using DataWranglerCommon.CameraHandling;

namespace DataWranglerInterface.ShotRecording
{
    /// <summary>
    /// Interaction logic for CameraInfoDebugControl.xaml
    /// </summary>
    public partial class CameraInfoDebugControl : UserControl
	{
		private ActiveCameraInfo? m_activeCamera = null;

		public CameraInfoDebugControl()
		{
			InitializeComponent();
		}

		public void SetTargetCamera(ActiveCameraInfo? a_activeCamera)
		{
			m_activeCamera = a_activeCamera;
		}
	}
}
