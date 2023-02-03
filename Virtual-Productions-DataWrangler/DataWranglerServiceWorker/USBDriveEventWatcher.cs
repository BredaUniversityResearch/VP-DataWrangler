using System;
using System.Management;
using DataWranglerCommon;

namespace DataWranglerServiceWorker
{
	public class USBDriveEventWatcher: IDisposable
	{
		public class VolumeChangedEvent
		{
			public enum EEventType : ushort
			{
				Invalid = 0,
				DeviceArrival = 2,
				DeviceRemoval = 3
			};

			public string DriveRootPath = "";
			public EEventType EventType = EEventType.Invalid;
		}

		private ManagementEventWatcher m_volumeChangedEventWatcher;

		public delegate void VolumeChangedDelegate(VolumeChangedEvent e);
		public event VolumeChangedDelegate OnVolumeChanged = delegate { };

		public USBDriveEventWatcher()
		{
			WqlEventQuery insertQuery = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2 OR EventType = 3");
			m_volumeChangedEventWatcher = new ManagementEventWatcher(insertQuery);
			m_volumeChangedEventWatcher.EventArrived += OnVolumeChangedEvent;
			//insertWatcher.EventArrived += new EventArrivedEventHandler(DeviceInsertedEvent);
			m_volumeChangedEventWatcher.Start();
		}

		public void Dispose()
		{
			m_volumeChangedEventWatcher.Dispose();
		}

		private void OnVolumeChangedEvent(object a_sender, EventArrivedEventArgs a_e)
		{
			VolumeChangedEvent evt = new VolumeChangedEvent { DriveRootPath = (string)a_e.NewEvent["DriveName"], EventType = (VolumeChangedEvent.EEventType)a_e.NewEvent["EventType"]};
			OnVolumeChanged.Invoke(evt);
		}

		public void DetectCurrentlyPresentUSBDrives()
		{
			ManagementObjectSearcher usbDriveQuery = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
			foreach (ManagementBaseObject usbDriveObject in usbDriveQuery.Get())
			{
				string mediaType = (string)usbDriveObject["MediaType"];
				// Possible mediaType values: https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-diskdrive#:~:text=be%20TRUE.-,MediaType,-Data%20type%3A
				// External hard disk media
				// Removable media("Removable media other than floppy")
				// Fixed hard disk("Fixed hard disk media")
				// Unknown("Format is unknown")
				if (mediaType.Contains("External"))
				{
					// associate physical disks with partitions
					ManagementObjectCollection partitionCollection = new ManagementObjectSearcher($"associators of {{Win32_DiskDrive.DeviceID='{usbDriveObject["DeviceID"]}'}} where AssocClass = Win32_DiskDriveToDiskPartition").Get();

					foreach (ManagementBaseObject partition in partitionCollection)
					{
						ManagementObjectCollection logicalCollection = new ManagementObjectSearcher($"associators of {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} where AssocClass= Win32_LogicalDiskToPartition").Get();
						foreach (ManagementBaseObject logical in logicalCollection)
						{
							ManagementObjectCollection volumeEnumerator = new ManagementObjectSearcher($"select DeviceID from Win32_LogicalDisk where Name='{logical["Name"]}'").Get();
							foreach (var volume in volumeEnumerator)
							{
								string volumeId = (string)volume["DeviceID"];
								VolumeChangedEvent evt = new VolumeChangedEvent() {DriveRootPath = volumeId, EventType = VolumeChangedEvent.EEventType.DeviceArrival };
								OnVolumeChanged.Invoke(evt);
							}
						}
					}
				}
			}
		}
	}
}
