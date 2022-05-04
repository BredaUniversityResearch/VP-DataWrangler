using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using BlackmagicCameraControl;
using BlackmagicCameraControl.CommandPackets;
using DataWranglerInterface.DebugSupport;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for CameraInfoDisplay.xaml
	/// </summary>
	public partial class CameraInfoDisplay : UserControl
	{
		private ActiveCameraInfo? m_targetCamera;
		public ActiveCameraInfo? TargetCameraInfo => m_targetCamera;

		public CameraInfoDisplay()
		{
			InitializeComponent();
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
			}

			Dispatcher.InvokeAsync(() =>
				{
					LoadingSpinner.Visibility = (m_targetCamera == null)? Visibility.Hidden : Visibility.Visible;
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
					CameraBattery.Content =
						$"{m_targetCamera.BatteryPercentage}% ({m_targetCamera.BatteryVoltage_mV} mV)";
				});
			}
			else if (a_e.PropertyName == nameof(ActiveCameraInfo.CurrentTransportMode))
			{
				Dispatcher.InvokeAsync(() => CameraState.Content = m_targetCamera.CurrentTransportMode.ToString() );
			}
			else if (a_e.PropertyName == nameof(ActiveCameraInfo.CurrentStorageTarget))
			{
				Dispatcher.InvokeAsync(() => CameraStorageTarget.Content = m_targetCamera.CurrentStorageTarget.ToString());
			}
		}
	}
}
