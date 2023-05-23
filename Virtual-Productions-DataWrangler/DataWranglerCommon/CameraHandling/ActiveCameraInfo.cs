using System.ComponentModel;
using AutoNotify;
using BlackmagicCameraControlData;
using BlackmagicCameraControlData.CommandPackets;
using CommonLogging;

namespace DataWranglerCommon.CameraHandling
{
    public class CameraPropertyChangedEventArgs : PropertyChangedEventArgs
    {
        public readonly ActiveCameraInfo Source;
        public readonly TimeCode ReceiveTimeCode;

        public CameraPropertyChangedEventArgs(string? a_propertyName, ActiveCameraInfo a_source, TimeCode a_receiveTimeCode)
            : base(a_propertyName)
        {
	        Source = a_source;
            ReceiveTimeCode = a_receiveTimeCode;
        }
    };

    public delegate void CameraPropertyChangedEventHandler(object? sender, CameraPropertyChangedEventArgs e);

    //A 'virtual' camera that can be represented by multiple connections (e.g. Bluetooth and SDI) but route to the same physical device
    public partial class ActiveCameraInfo
    {
	    private readonly List<CameraDeviceHandle> m_connectionsForPhysicalDevice = new List<CameraDeviceHandle>();
        public IReadOnlyCollection<CameraDeviceHandle> ConnectionsForPhysicalDevice => m_connectionsForPhysicalDevice;
        public ConfigActiveCameraGrouping? Grouping = null;

        public delegate void ConnectionCollectionChanged(ActiveCameraInfo a_source);
        public event ConnectionCollectionChanged OnDeviceConnectionsChanged = delegate { };

        private DateTimeOffset m_connectTime;
        private DateTime m_timeSyncPoint = DateTime.MinValue;

        private bool m_receivedAnyBatteryStatusPackets = false;
        public event CameraPropertyChangedEventHandler? CameraPropertyChanged;
        private CameraPropertyCache m_cameraProperties = new CameraPropertyCache();

        [AutoNotify]
        private string m_cameraName = "";
        [AutoNotify]
        private string m_cameraModel = "";
		[AutoNotify]
		private string m_cameraNumber = "A";
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

        public ActiveCameraInfo(ConfigActiveCameraGrouping? a_grouping)
        {
	        Grouping = a_grouping;
            m_connectTime = DateTimeOffset.UtcNow;
        }

        public void TransferCameraHandle(ActiveCameraInfo? a_fromCamera, CameraDeviceHandle a_deviceHandle)
        {
	        if (a_fromCamera != null)
	        {
		        bool success = a_fromCamera.m_connectionsForPhysicalDevice.Remove(a_deviceHandle);
		        if (!success)
		        {
			        throw new Exception("Failed to remove camera handle from source camera info");
		        }

		        a_fromCamera.RemoveCameraDeviceHandleFromGrouping(a_deviceHandle);
		        
	        }

	        m_connectionsForPhysicalDevice.Add(a_deviceHandle);

	        AddCameraToGrouping(a_deviceHandle);

	        if (a_fromCamera != null)
	        {
		        foreach (var kvp in a_fromCamera.m_cameraProperties.CurrentValues)
		        {
			        if (m_cameraProperties.CheckPropertyChanged(kvp.Key, kvp.Value.PacketData,
				            kvp.Value.LastUpdateTime))
			        {
				        OnUpdatedCameraDataReceived(a_deviceHandle, kvp.Value.LastUpdateTime, kvp.Value.PacketData);
			        }
		        }

		        a_fromCamera.OnDeviceConnectionsChanged.Invoke(a_fromCamera);
	        }

	        OnDeviceConnectionsChanged.Invoke(this);
        }

        private void AddCameraToGrouping(CameraDeviceHandle a_deviceHandle)
        {
			if (m_connectionsForPhysicalDevice.Count > 1)
			{
				if (Grouping == null)
				{
					Grouping = new ConfigActiveCameraGrouping
					{
						Name = CameraName
					};
					foreach (CameraDeviceHandle handle in m_connectionsForPhysicalDevice)
					{
						Grouping.DeviceHandleUuids.Add(handle.DeviceUuid);
					}
				}
				else
				{
					if (!Grouping.DeviceHandleUuids.Contains(a_deviceHandle.DeviceUuid))
					{
						Grouping.DeviceHandleUuids.Add(a_deviceHandle.DeviceUuid);
					}
				}
			}
		}

        private void RemoveCameraDeviceHandleFromGrouping(CameraDeviceHandle a_deviceHandle)
        {
			if (Grouping != null)
			{
				if (m_connectionsForPhysicalDevice.Count <= 1)
				{
					Grouping = null;
				}
				else
				{
					Grouping.DeviceHandleUuids.Remove(a_deviceHandle.DeviceUuid);
				}
			}
		}

        public void OnCameraDataReceived(CameraDeviceHandle a_deviceHandle, TimeCode a_receivedTime, ICommandPacketBase a_packet)
        {
	        if (!m_cameraProperties.CheckPropertyChanged(a_packet, a_receivedTime))
            {
                return;
            }

	        OnUpdatedCameraDataReceived(a_deviceHandle, a_receivedTime, a_packet);
        }

        private void OnUpdatedCameraDataReceived(CameraDeviceHandle a_deviceHandle, TimeCode a_receivedTime, ICommandPacketBase a_packet)
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
			        a_deviceHandle.TargetController.TrySendAsyncCommand(a_deviceHandle, tzPacket);
			        m_timeSyncPoint = DateTime.UtcNow;
			        a_deviceHandle.TargetController.TrySendAsyncCommand(a_deviceHandle,
				        new CommandPacketConfigurationRealTimeClock(m_timeSyncPoint));

			        Logger.LogInfo("CameraInfo",
				        $"Synchronizing camera with deviceHandle {a_deviceHandle.DeviceUuid} time to {DateTime.UtcNow} + {tzPacket.MinutesOffsetFromUTC} Minutes");

			        m_receivedAnyBatteryStatusPackets = true;
		        }

		        if (SelectedCodec == "" && DateTimeOffset.UtcNow - m_connectTime > new TimeSpan(0, 0, 15))
		        {
			        a_deviceHandle.TargetController.TrySendAsyncCommand(a_deviceHandle,
				        new CommandPacketMediaCodec()
					        { BasicCodec = CommandPacketMediaCodec.EBasicCodec.BlackmagicRAW, Variant = 0 });
		        }

		        BatteryPercentage = batteryInfo.BatteryPercentage;
		        OnCameraPropertyChanged(nameof(BatteryPercentage), a_receivedTime);
		        BatteryVoltage_mV = batteryInfo.BatteryVoltage_mV;
		        OnCameraPropertyChanged(nameof(BatteryVoltage_mV), a_receivedTime);
	        }
	        else if (a_packet is CommandPacketMediaTransportMode transportMode)
	        {
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
            CameraPropertyChangedEventArgs evt = new CameraPropertyChangedEventArgs(a_propertyName, this, a_changeTime);
            CameraPropertyChanged?.Invoke(this, evt);
        }
    }
}
