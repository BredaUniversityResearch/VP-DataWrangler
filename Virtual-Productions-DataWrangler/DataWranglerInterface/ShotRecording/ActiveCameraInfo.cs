using System.ComponentModel;
using AutoNotify;
using BlackmagicCameraControl.CommandPackets;
using BlackmagicCameraControlData;
using CommonLogging;
using DataWranglerCommon;

namespace DataWranglerInterface.ShotRecording
{
	public class CameraPropertyChangedEventArgs: PropertyChangedEventArgs
	{
		public readonly TimeCode ReceiveTimeCode;

		public CameraPropertyChangedEventArgs(string? a_propertyName, TimeCode a_receiveTimeCode) 
			: base(a_propertyName)
		{
			ReceiveTimeCode = a_receiveTimeCode;
		}
	};

	public delegate void CameraPropertyChangedEventHandler(object? sender, CameraPropertyChangedEventArgs e);

	//A 'virtual' camera that can be represented by multiple connections (e.g. Bluetooth and SDI) but route to the same physical device
	public partial class ActiveCameraInfo
	{
		private readonly List<CameraDeviceHandle> m_connectionsForPhysicalDevice = new List<CameraDeviceHandle>();
		public IReadOnlyCollection<CameraDeviceHandle> ConnectionsForPhysicalDevice => m_connectionsForPhysicalDevice;

		public delegate void OnConnectionCollectionChanged(ActiveCameraInfo a_source);
		public event OnConnectionCollectionChanged DeviceConnectionsChanged = delegate { };

		private DateTimeOffset m_connectTime;
		private DateTime m_timeSyncPoint = DateTime.MinValue;

		private bool m_receivedAnyBatteryStatusPackets = false;
		public event CameraPropertyChangedEventHandler? CameraPropertyChanged;

		[AutoNotify]
		private string m_cameraName = "";
		[AutoNotify]
		private string m_cameraModel = "";
		[AutoNotify]
		private int m_batteryPercentage = 0;
		[AutoNotify]
		private int m_batteryVoltage_mV;
		[AutoNotify]
		private CommandPacketMediaTransportMode.EMode m_currentTransportMode;
		[AutoNotify]
		private string m_selectedCodec = ""; //String representation of the basic coded selected by camera.
		[AutoNotify]
		private TimeCode m_currentTimeCode;

		public ActiveCameraInfo(CameraDeviceHandle a_deviceHandle)
		{
			m_connectionsForPhysicalDevice.Add(a_deviceHandle);
			m_connectTime = DateTimeOffset.UtcNow;
		}

		public void TransferCameraHandle(ActiveCameraInfo a_fromCamera, CameraDeviceHandle a_deviceHandle)
		{
			bool success = a_fromCamera.m_connectionsForPhysicalDevice.Remove(a_deviceHandle);
			if (!success)
			{
				throw new Exception("Failed to remove camera handle from source camera info");
			}

			m_connectionsForPhysicalDevice.Add(a_deviceHandle);

			a_fromCamera.DeviceConnectionsChanged.Invoke(a_fromCamera);
			DeviceConnectionsChanged.Invoke(this);
		}

		public void OnCameraDataReceived(CameraControllerBase a_deviceController, CameraDeviceHandle a_deviceHandle, TimeCode a_receivedTime, ICommandPacketBase a_packet)
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
					a_deviceController.TrySendAsyncCommand(a_deviceHandle, tzPacket);
					m_timeSyncPoint = DateTime.UtcNow;
					a_deviceController.TrySendAsyncCommand(a_deviceHandle, new CommandPacketConfigurationRealTimeClock(m_timeSyncPoint));

					Logger.LogInfo("CameraInfo", $"Synchronizing camera with deviceHandle {a_deviceHandle.DeviceUuid} time to {DateTime.UtcNow} + {tzPacket.MinutesOffsetFromUTC} Minutes");

					m_receivedAnyBatteryStatusPackets = true;
				}

				if (SelectedCodec == "" && DateTimeOffset.UtcNow - m_connectTime > new TimeSpan(0, 0, 15))
				{
					a_deviceController.TrySendAsyncCommand(a_deviceHandle,
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
			else if (a_packet is CommandPacketSystemTimeCode timeCodeChanged)
			{
				CurrentTimeCode = timeCodeChanged.TimeCode;
				OnCameraPropertyChanged(nameof(CurrentTimeCode), a_receivedTime);
			}
		}

		private void OnCameraPropertyChanged(string a_propertyName, TimeCode a_changeTime)
		{
			CameraPropertyChangedEventArgs evt = new CameraPropertyChangedEventArgs(a_propertyName, a_changeTime);
			//PropertyChanged?.Invoke(this, evt);
			CameraPropertyChanged?.Invoke(this, evt);
		}
	}
}
