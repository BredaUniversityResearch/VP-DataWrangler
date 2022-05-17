﻿using System;
using System.Management;
using DataWranglerCommon;

namespace DataWranglerServiceWorker
{
	public class USBDriveEventWatcher: IDisposable
	{
		private class VolumeChangedEvent
		{
			public enum EEventType : ushort
			{
				Invalid = 0,
				DeviceArrival = 2,
				DeviceRemoval = 3
			};

			public string DriveName = "";
			public EEventType EventType = EEventType.Invalid;
		}

		private ManagementEventWatcher m_volumeChangedEventWatcher;

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
			VolumeChangedEvent evt = new VolumeChangedEvent { DriveName = (string)a_e.NewEvent["DriveName"], EventType = (VolumeChangedEvent.EEventType)a_e.NewEvent["EventType"]};

			Logger.LogInfo("USBDriveWatcher", a_e.NewEvent.GetText(TextFormat.Mof));
		}

	}
}
