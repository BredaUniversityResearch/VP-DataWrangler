using System.Windows;
using System.Windows.Controls;
using BlackmagicCameraControl.CommandPackets;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for CameraInfoControl.xaml
	/// </summary>
	public partial class CameraInfoControl : UserControl
	{
		private ActiveCameraInfo? m_targetCamera;
		public ActiveCameraInfo? TargetCameraInfo => m_targetCamera;

		public delegate void RecordingStateChangedHandler(ActiveCameraInfo a_camera, bool a_isNowRecording, DateTimeOffset a_stateChangeTime);
		public event RecordingStateChangedHandler OnCameraRecordingStateChanged = delegate { };

		public CameraInfoControl()
		{
			InitializeComponent();

			CameraStorageTarget.OnStorageTargetChanged += OnStorageTargetChangedInUI;
		}

		public void SetTargetCameraInfo(ActiveCameraInfo? a_activeCamera)
		{
			if (m_targetCamera != null)
			{
				m_targetCamera.CameraPropertyChanged -= OnCameraPropertyChanged;
			}

			m_targetCamera = a_activeCamera;
			if (m_targetCamera != null)
			{
				m_targetCamera.CameraPropertyChanged += OnCameraPropertyChanged;

				if (string.IsNullOrEmpty(m_targetCamera.CurrentStorageTarget))
				{
					m_targetCamera.SetStorageTarget(CameraStorageTarget.StorageTargetString);
				}
			}

			Dispatcher.InvokeAsync(() =>
				{
					LoadingSpinner.Visibility = (m_targetCamera == null)? Visibility.Visible : Visibility.Hidden;
				}
			);
		}

		private void OnCameraPropertyChanged(object? a_sender, CameraPropertyChangedEventArgs a_e)
		{
			if (m_targetCamera == null)
			{
				throw new Exception();
			}

			if (a_e.PropertyName == nameof(ActiveCameraInfo.CameraName))
			{
				Dispatcher.InvokeAsync(() =>
					{
						CameraDisplayName.Content = m_targetCamera.CameraName;
					}
				);
			}
			else if (a_e.PropertyName == nameof(ActiveCameraInfo.CameraModel))
			{
				Dispatcher.InvokeAsync(() => { CameraModel.Content = m_targetCamera.CameraModel; });

			}
			else if (a_e.PropertyName == nameof(ActiveCameraInfo.BatteryPercentage))
			{
				Dispatcher.InvokeAsync(() =>
				{
					if (m_targetCamera == null)
					{
						return;
					}

					CameraBattery.Content =
						$"{m_targetCamera.BatteryPercentage}% ({m_targetCamera?.BatteryVoltage_mV} mV)";
				});
			}
			else if (a_e.PropertyName == nameof(ActiveCameraInfo.CurrentTransportMode))
			{
				Dispatcher.InvokeAsync(() => CameraState.Content = m_targetCamera.CurrentTransportMode.ToString() );
				bool isNowRecording = m_targetCamera.CurrentTransportMode == CommandPacketMediaTransportMode.EMode.Record;
				OnCameraRecordingStateChanged.Invoke(m_targetCamera, isNowRecording, a_e.ReceivedChangeTime);
			}
			else if (a_e.PropertyName == nameof(ActiveCameraInfo.CurrentStorageTarget))
			{
				Dispatcher.InvokeAsync(() => CameraStorageTarget.StorageTargetString = m_targetCamera.CurrentStorageTarget);
			}
			else if (a_e.PropertyName == nameof(ActiveCameraInfo.CurrentTimeCode))
			{
				Dispatcher.InvokeAsync(() => CameraTimeCode.Content = m_targetCamera.CurrentTimeCode);
			}

		}

		private void OnStorageTargetChangedInUI(string a_obj)
		{
			if (m_targetCamera != null)
			{
				m_targetCamera.SetStorageTarget(a_obj);
			}
		}
	}
}
