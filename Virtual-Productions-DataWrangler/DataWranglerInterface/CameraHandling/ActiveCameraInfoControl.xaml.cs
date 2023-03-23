using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using BlackmagicCameraControlData;
using DataWranglerInterface.CameraHandling;

namespace DataWranglerInterface.ShotRecording
{
    /// <summary>
    /// Interaction logic for ActiveCameraInfoControl.xaml
    /// </summary>
    public partial class ActiveCameraInfoControl : UserControl
	{
		public ActiveCameraInfo TargetInfo { get; private set; }
		public string CurrentTooltip => $"Virtual camera\nModel: {TargetInfo.CameraModel}\nName: {TargetInfo.CameraName}\nBattery: {TargetInfo.BatteryPercentage}({TargetInfo.BatteryVoltage_mV} mV)\nCodec: {TargetInfo.SelectedCodec}\nTransport: {TargetInfo.CurrentTransportMode}";

		public ObservableCollection<CameraDeviceHandleControl> DeviceHandleControls { get; } = new ObservableCollection<CameraDeviceHandleControl>();

		public ActiveCameraInfoControl(ActiveCameraInfo a_targetInfo)
		{
			TargetInfo = a_targetInfo;

			foreach (CameraDeviceHandle handle in TargetInfo.ConnectionsForPhysicalDevice)
			{
				DeviceHandleControls.Add(new CameraDeviceHandleControl(TargetInfo, handle));
			}

			TargetInfo.PropertyChanged += OnTargetPropertyChanged;
			TargetInfo.DeviceConnectionsChanged += OnTargetDeviceConnectionsChanged;

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
			if (a_e.Data.GetData(typeof(CameraDeviceHandleControl.DragDropInfo)) is CameraDeviceHandleControl.DragDropInfo deviceHandleData)
			{
				TargetInfo.TransferCameraHandle(deviceHandleData.SourceCameraInfo, deviceHandleData.SourceDeviceHandle);
			}
		}

		private void OnTargetDeviceConnectionsChanged(ActiveCameraInfo a_source)
		{
			HashSet<CameraDeviceHandle> newConnections = new HashSet<CameraDeviceHandle>(TargetInfo.ConnectionsForPhysicalDevice);
			HashSet<CameraDeviceHandle> oldConnections = new HashSet<CameraDeviceHandle>();
			foreach (CameraDeviceHandleControl control in DeviceHandleControls)
			{
				oldConnections.Add(control.DeviceHandle);
			}

			HashSet<CameraDeviceHandle> added = new HashSet<CameraDeviceHandle>(newConnections);
			added.ExceptWith(oldConnections);
			foreach (CameraDeviceHandle addedHandle in added)
			{
				DeviceHandleControls.Add(new CameraDeviceHandleControl(TargetInfo, addedHandle));
			}

			HashSet<CameraDeviceHandle> removed = new HashSet<CameraDeviceHandle>(oldConnections);
			removed.ExceptWith(newConnections);

			foreach (CameraDeviceHandle removedHandle in removed)
			{
				for (int i = DeviceHandleControls.Count - 1; i >= 0; --i)
				{
					if (DeviceHandleControls[i].DeviceHandle == removedHandle)
					{
						DeviceHandleControls.RemoveAt(i);
						break;
					}
				}
			}
		}

		private void OnToolTipOpening(object a_sender, ToolTipEventArgs a_e)
		{
			((StackPanel)a_sender).ToolTip = CurrentTooltip;
		}
	}
}
