using System.ComponentModel;
using System.Runtime.CompilerServices;
using BlackmagicCameraControl;
using BlackmagicCameraControl.CommandPackets;
using BlackmagicCameraControlBluetooth;
using BlackmagicCameraControlData;
using CommonLogging;
using DataWranglerCommon;
using DataWranglerInterface.DebugSupport;

namespace DataWranglerInterface.ShotRecording
{
	public class CameraPropertyChangedEventArgs: PropertyChangedEventArgs
	{
		public readonly DateTimeOffset ReceivedChangeTime;

		public CameraPropertyChangedEventArgs(string? a_propertyName, DateTimeOffset a_bluetoothChangeTime) 
			: base(a_propertyName)
		{
			ReceivedChangeTime = a_bluetoothChangeTime;
		}
	};

	public delegate void CameraPropertyChangedEventHandler(object? sender, CameraPropertyChangedEventArgs e);

	public class ActiveCameraInfo: INotifyPropertyChanged
	{
		public readonly CameraHandle TargetCamera;
		private DateTimeOffset m_connectTime;
		private DateTime m_timeSyncPoint = DateTime.MinValue;

		private bool m_receivedAnyBatteryStatusPackets = false;
		public event PropertyChangedEventHandler? PropertyChanged;
		public event CameraPropertyChangedEventHandler? CameraPropertyChanged;

		public string CameraName { get; private set; } = "";
		public string CameraModel { get; private set; } = "";
		public int BatteryPercentage { get; private set; }
		// ReSharper disable once InconsistentNaming
		public int BatteryVoltage_mV { get; private set; }
		public CommandPacketMediaTransportMode.EMode CurrentTransportMode { get; private set; }
		public string CurrentStorageTarget { get; private set; } = "";
		public string SelectedCodec { get; private set; } = ""; //String representation of the basic coded selected by camera.
		public TimeCode CurrentTimeCode { get; private set; }
		
		public ActiveCameraInfo(CameraHandle a_handle)
		{
			TargetCamera = a_handle;
			m_connectTime = DateTimeOffset.UtcNow;
		}

		public void OnCameraDataReceived(BlackmagicBluetoothCameraAPIController a_controller, DateTimeOffset a_receivedTime, ICommandPacketBase a_packet)
		{
			if (a_packet is CommandPacketCameraModel modelPacket)
			{
				CameraModel = modelPacket.CameraModel;
				OnCameraPropertyChanged(nameof(CameraModel), a_receivedTime);
			}
			else if (a_packet is CommandPacketSystemBatteryInfo batteryInfo)
			{
				if (!m_receivedAnyBatteryStatusPackets)
				{
					CommandPacketConfigurationTimezone tzPacket = new CommandPacketConfigurationTimezone(TimeZoneInfo.Local);
					a_controller.AsyncSendCommand(TargetCamera, tzPacket);
					m_timeSyncPoint = DateTime.UtcNow;
					a_controller.AsyncSendCommand(TargetCamera, new CommandPacketConfigurationRealTimeClock(m_timeSyncPoint));

					Logger.LogInfo("CameraInfo", $"Synchronizing camera with handle {TargetCamera.ConnectionId} time to {DateTime.UtcNow} + {tzPacket.MinutesOffsetFromUTC} Minutes");

					m_receivedAnyBatteryStatusPackets = true;
				}

				if (SelectedCodec == "" && DateTimeOffset.UtcNow - m_connectTime > new TimeSpan(0, 0, 15))
				{
					a_controller.AsyncSendCommand(TargetCamera,
						new CommandPacketMediaCodec()
							{BasicCodec = CommandPacketMediaCodec.EBasicCodec.BlackmagicRAW, Variant = 0});
				}

				BatteryPercentage = batteryInfo.BatteryPercentage;
				OnCameraPropertyChanged(nameof(BatteryPercentage), a_receivedTime);
				BatteryVoltage_mV = batteryInfo.BatteryVoltage_mV;
				OnCameraPropertyChanged(nameof(BatteryVoltage_mV), a_receivedTime);
			}
			else if (a_packet is CommandPacketMediaTransportMode transportMode)
			{
				//Note to self: Timestamp for created and modified is just the start time. Seems to be off by ~2 seconds.
				CurrentTransportMode = transportMode.Mode;
				OnCameraPropertyChanged(nameof(CurrentTransportMode), a_receivedTime);

				Logger.LogInfo("CameraInfo", $"Transport mode changed to {transportMode.Mode} at {a_receivedTime}");
			}
			else if (a_packet is CommandPacketMediaCodec codecPacket)
			{
				Logger.LogInfo("CameraInfo", $"Codec changed to: {codecPacket.BasicCodec}:{codecPacket.Variant}");
				SelectedCodec = codecPacket.BasicCodec.ToString();
				OnCameraPropertyChanged(nameof(CurrentTransportMode), a_receivedTime);
			}
			else if (a_packet is CommandPacketVendorStorageTargetChanged storageTarget)
			{
				Logger.LogInfo("CameraInfo", $"Storage target changed to: {storageTarget.StorageTargetName}");

				CurrentStorageTarget = storageTarget.StorageTargetName;
				OnCameraPropertyChanged(nameof(CurrentStorageTarget), a_receivedTime);
			}
			else if (a_packet is CommandPacketSystemTimeCode timeCodeChanged)
			{
				CurrentTimeCode = timeCodeChanged.TimeCode;
				OnCameraPropertyChanged(nameof(CurrentTimeCode), a_receivedTime);
			}
		}

		protected virtual void OnPropertyChanged([CallerMemberName] string? a_propertyName = null)
		{
			throw new NotImplementedException();
		}

		private void OnCameraPropertyChanged(string a_propertyName, DateTimeOffset a_changeTime)
		{
			CameraPropertyChangedEventArgs evt = new CameraPropertyChangedEventArgs(a_propertyName, a_changeTime);
			PropertyChanged?.Invoke(this, evt);
			CameraPropertyChanged?.Invoke(this, evt);
		}

		public void SetStorageTarget(string a_storageTargetName)
		{
			if (CurrentStorageTarget != a_storageTargetName)
			{
				CurrentStorageTarget = a_storageTargetName;
				OnCameraPropertyChanged(nameof(CurrentStorageTarget), DateTimeOffset.UtcNow);
			}
		}
	}
}
