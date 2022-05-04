using System.ComponentModel;
using System.Runtime.CompilerServices;
using BlackmagicCameraControl;
using BlackmagicCameraControl.CommandPackets;
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
		private CameraHandle m_targetCamera;
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

		public ActiveCameraInfo(CameraHandle a_handle)
		{
			m_targetCamera = a_handle;
		}

		public void OnCameraDataReceived(BlackmagicCameraAPIController a_controller, DateTimeOffset a_receivedTime, ICommandPacketBase a_packet)
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
					a_controller.AsyncSendCommand(m_targetCamera, tzPacket);
					m_timeSyncPoint = DateTime.UtcNow;
					a_controller.AsyncSendCommand(m_targetCamera, new CommandPacketConfigurationRealTimeClock(m_timeSyncPoint));

					IBlackmagicCameraLogInterface.LogInfo(
						$"Synchronizing camera with handle {m_targetCamera.ConnectionId} time to {DateTime.UtcNow} + {tzPacket.MinutesOffsetFromUTC} Minutes");

					m_receivedAnyBatteryStatusPackets = true;
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
			else if (a_packet is CommandPacketVendorStorageTargetChanged storageTarget)
			{
				IBlackmagicCameraLogInterface.LogVerbose($"Storage target changed to: {storageTarget.StorageTargetName}");

				CurrentStorageTarget = storageTarget.StorageTargetName;
				OnCameraPropertyChanged(nameof(CurrentStorageTarget), a_receivedTime);
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
	}
}
