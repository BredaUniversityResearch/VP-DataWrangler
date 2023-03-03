using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using BlackmagicCameraControl.CommandPackets;
using DataWranglerCommon;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for CameraInfoControl.xaml
	/// </summary>
	public partial class CameraInfoControl : UserControl
	{
		public ObservableCollection<ActiveCameraInfoControl> ActiveCameras { get; private set; } = new ObservableCollection<ActiveCameraInfoControl>();

		public delegate void RecordingStateChangedHandler(ActiveCameraInfo a_camera, bool a_isNowRecording, TimeCode a_stateChangeTime);
		public event RecordingStateChangedHandler OnCameraRecordingStateChanged = delegate { };

		public CameraInfoControl()
		{
			InitializeComponent();
		}

		public void AddTargetCameraInfo(ActiveCameraInfo a_activeCamera)
		{
			Dispatcher.Invoke(() =>
			{
				ActiveCameraInfoControl control = new ActiveCameraInfoControl(a_activeCamera);
				ActiveCameras.Add(control);
			});
			//a_activeCamera.CameraPropertyChanged += OnCameraPropertyChanged;
		}

		public void RemoveTargetCameraInfo(ActiveCameraInfo a_handle)
		{
			for (int i = ActiveCameras.Count - 1; i >= 0; --i)
			{
				if (ActiveCameras[i].TargetInfo == a_handle)
				{
					ActiveCameras.RemoveAt(i);
					break;
				}
			}
		}
	}
}
