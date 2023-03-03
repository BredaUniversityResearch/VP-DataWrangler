using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using BlackmagicCameraControlData;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for ActiveCameraInfoControl.xaml
	/// </summary>
	public partial class ActiveCameraInfoControl : UserControl
	{
		public ActiveCameraInfo TargetInfo { get; private set; }
		public string CurrentTooltip => $"Virtual camera {TargetInfo.CameraModel}";

		public ObservableCollection<CameraDeviceHandleControl> DeviceHandleControls { get; } = new ObservableCollection<CameraDeviceHandleControl>();

		public ActiveCameraInfoControl(ActiveCameraInfo a_targetInfo)
		{
			TargetInfo = a_targetInfo;

			foreach (CameraDeviceHandle handle in TargetInfo.ConnectionsForPhysicalDevice)
			{
				DeviceHandleControls.Add(new CameraDeviceHandleControl(handle));
			}

			TargetInfo.PropertyChanged += OnTargetPropertyChanged;

			InitializeComponent();

			DeviceHandleControl.Drop += OnDeviceHandleDragDrop;
		}

		private void OnTargetPropertyChanged(object? a_sender, PropertyChangedEventArgs a_e)
		{
			if (a_e.PropertyName == nameof(TargetInfo.ConnectionsForPhysicalDevice))
			{
				throw new NotImplementedException();
			}
		}

		private void OnCameraPropertyChanged(object? a_sender, CameraPropertyChangedEventArgs a_e)
		{

			if (a_e.PropertyName == nameof(ActiveCameraInfo.CameraName))
			{
				Dispatcher.InvokeAsync(() =>
				{
					//CameraDisplayName.Content = m_targetCamera.CameraName;
				}
				);
			}
			else if (a_e.PropertyName == nameof(ActiveCameraInfo.CameraModel))
			{
				//Dispatcher.InvokeAsync(() => { CameraModel.Content = m_targetCamera.CameraModel; });

			}
			else if (a_e.PropertyName == nameof(ActiveCameraInfo.BatteryPercentage))
			{
				Dispatcher.InvokeAsync(() =>
				{
					//CameraBattery.Content =
					//	$"{m_targetCamera.BatteryPercentage}% ({m_targetCamera?.BatteryVoltage_mV} mV)";
				});
			}
			else if (a_e.PropertyName == nameof(ActiveCameraInfo.CurrentTransportMode))
			{
				//Dispatcher.InvokeAsync(() => CameraState.Content = m_targetCamera.CurrentTransportMode.ToString() );
				//bool isNowRecording = m_targetCamera.CurrentTransportMode == CommandPacketMediaTransportMode.EMode.Record;
				//OnCameraRecordingStateChanged.Invoke(m_targetCamera, isNowRecording, a_e.ReceiveTimeCode);
			}
			else if (a_e.PropertyName == nameof(ActiveCameraInfo.CurrentTimeCode))
			{
				//Dispatcher.InvokeAsync(() => CameraTimeCode.Content = m_targetCamera.CurrentTimeCode);
			}

		}

		private void OnDeviceHandleDragDrop(object a_sender, DragEventArgs a_e)
		{
			throw new NotImplementedException();
		}

	}
}
