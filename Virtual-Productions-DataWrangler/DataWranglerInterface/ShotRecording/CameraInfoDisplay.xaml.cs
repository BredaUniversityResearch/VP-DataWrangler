using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using BlackmagicCameraControl;
using BlackmagicCameraControl.CommandPackets;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for CameraInfoDisplay.xaml
	/// </summary>
	public partial class CameraInfoDisplay : UserControl
	{
		private BlackmagicCameraController? m_controller = null;
		private CameraHandle m_targetCamera = CameraHandle.Invalid;

		public CameraInfoDisplay()
		{
			InitializeComponent();
		}

		public void SetController(BlackmagicCameraController a_controller)
		{
			m_controller = a_controller;
			m_controller.OnCameraConnected += OnCameraConnected;
			m_controller.OnCameraDataReceived += OnCameraDataReceived;
		}

		private void OnCameraConnected(CameraHandle a_handle)
		{
			if (m_controller == null)
			{
				throw new Exception();
			}
			string cameraDisplayName = m_controller.GetBluetoothName(a_handle);
			Dispatcher.InvokeAsync(() =>
				{
					LoadingSpinner.Visibility = Visibility.Hidden;
					CameraDisplayName.Content = cameraDisplayName;
				}
			);

			//m_controller.AsyncRequestCameraName(a_handle);
			m_controller.AsyncRequestCameraModel(a_handle);
			Debug.Fail("TODO: Set RTC & Timezone offset to known value, preferably value from machine which should be NTP synchronized.");
			m_controller.AsyncSendCommand(a_handle, new CommandConfigurationRealTimeClock());

			m_targetCamera = a_handle;
		}

		private void OnCameraDataReceived(CameraHandle a_handle, ICommandPacketBase a_packet)
		{
			if (a_packet is CommandPacketCameraModel modelPacket)
			{
				Dispatcher.InvokeAsync(() => { CameraModel.Content = modelPacket.CameraModel; });
			}
			else if (a_packet is CommandPacketBatteryInfo batteryInfo)
			{
				Dispatcher.InvokeAsync(() =>
				{
					CameraBattery.Content =
						$"{batteryInfo.BatteryPercentage}% ({batteryInfo.BatteryVoltage_mV} mV)";
				});
			}
			else if (a_packet is CommandMediaTransportMode transportMode)
			{
				Dispatcher.InvokeAsync(() =>
				{
					CameraState.Content = transportMode.Mode.ToString();
				});
			}
			else if (a_packet is CommandMediaCodec codecInfo)
			{
				int a = 7;
			}
			else if (a_packet is CommandConfigurationRealTimeClock rtcInfo)
			{
				Dispatcher.InvokeAsync(() => { CameraTime.Content = rtcInfo.ClockTime.ToLongTimeString(); });
			}
		}

		private void ButtonBase_OnClick(object a_sender, RoutedEventArgs a_e)
		{
			m_controller!.AsyncSendCommand(m_targetCamera, new CommandConfigurationRealTimeClock() { BinaryDateCode = 0x19961231, BinaryTimeCode = 0x13374201});
			//m_controller!.AsyncSendCommand(m_targetCamera,
			//	new CommandMediaTransportMode() {Mode = CommandMediaTransportMode.EMode.Record});
		}
	}
}
