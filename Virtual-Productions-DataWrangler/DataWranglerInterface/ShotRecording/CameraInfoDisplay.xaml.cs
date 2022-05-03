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
		private BlackmagicCameraController? m_controller = null;
		private CameraHandle m_targetCamera = CameraHandle.Invalid;
		private DateTime m_timeSyncPoint = DateTime.MinValue;

		private bool m_receivedAnyBatteryStatusPackets = false;

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
			m_targetCamera = a_handle;
		}

		private void OnCameraDataReceived(CameraHandle a_handle, DateTimeOffset a_receivedTime, ICommandPacketBase a_packet)
		{
			if (a_packet is CommandPacketCameraModel modelPacket)
			{
				Dispatcher.InvokeAsync(() => { CameraModel.Content = modelPacket.CameraModel; });
			}
			else if (a_packet is CommandPacketBatteryInfo batteryInfo)
			{
				if (!m_receivedAnyBatteryStatusPackets)
				{
					if (m_controller == null)
					{
						throw new Exception();
					}

					CommandPacketConfigurationTimezone tzPacket = new CommandPacketConfigurationTimezone(TimeZoneInfo.Local);
					m_controller.AsyncSendCommand(a_handle, tzPacket);
					m_timeSyncPoint = DateTime.UtcNow;
					m_controller.AsyncSendCommand(a_handle, new CommandPacketConfigurationRealTimeClock(m_timeSyncPoint));

					IBlackmagicCameraLogInterface.LogInfo(
						$"Synchronizing camera time to {DateTime.UtcNow} + {tzPacket.MinutesOffsetFromUTC} Minutes");

					m_receivedAnyBatteryStatusPackets = true;
				}

				Dispatcher.InvokeAsync(() =>
				{
					CameraBattery.Content =
						$"{batteryInfo.BatteryPercentage}% ({batteryInfo.BatteryVoltage_mV} mV)";
				});
			}
			else if (a_packet is CommandPacketMediaTransportMode transportMode)
			{
				//Note to self: Timestamp for created and modified is just the start time. Seems to be off by ~2 seconds.
				Logger.LogInfo("CameraInfo", $"Transport mode changed to {transportMode.Mode} at {a_receivedTime}");
				Dispatcher.InvokeAsync(() =>
				{
					CameraState.Content = transportMode.Mode.ToString();
				});
			}
			else if (a_packet is CommandPacketConfigurationRealTimeClock rtcInfo)
			{
				Dispatcher.InvokeAsync(() => { CameraTime.Content = rtcInfo.ClockTime.ToLongTimeString(); });
			}
			else if (a_packet is CommandPacketVendorStorageTargetChanged storageTarget)
			{
				IBlackmagicCameraLogInterface.LogVerbose($"!SPEC! Storage target changed to: {storageTarget.StorageTargetName}");
			}
		}

		private void ButtonBase_OnClick(object a_sender, RoutedEventArgs a_e)
		{
			m_controller!.AsyncSendCommand(m_targetCamera, new CommandPacketConfigurationRealTimeClock(DateTime.UtcNow));
			//m_controller!.AsyncSendCommand(m_targetCamera,
			//	new CommandMediaTransportMode() {Mode = CommandMediaTransportMode.EMode.Record});
		}
	}
}
