using System.Windows.Controls;
using System.Windows.Threading;
using BlackmagicCameraControl;
using BlackmagicCameraControlBluetooth;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for CameraSelectorControl.xaml
	/// </summary>
	public partial class CameraSelectorControl : UserControl
	{
		private class AdvertisedDeviceEntry
		{
			public readonly ulong DeviceId;
			public readonly string DeviceName;

			public AdvertisedDeviceEntry(ulong a_deviceId, string a_deviceName)
			{
				DeviceId = a_deviceId;
				DeviceName = a_deviceName;
			}

			protected bool Equals(AdvertisedDeviceEntry other)
			{
				return DeviceId == other.DeviceId;
			}

			public override bool Equals(object? obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != this.GetType()) return false;
				return Equals((AdvertisedDeviceEntry)obj);
			}

			public override int GetHashCode()
			{
				return DeviceId.GetHashCode();
			}

			public override string ToString()
			{
				return DeviceName;
			}
		};

		private DispatcherTimer m_updateTimer;

		private BlackmagicBluetoothCameraAPIController? m_cameraApiController = null;

		public delegate void BluetoothDeviceSelected(ulong a_bluetoothAddress);
		public event BluetoothDeviceSelected OnBluetoothDeviceSelected = delegate { };

		public CameraSelectorControl()
		{
			InitializeComponent();

			m_updateTimer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, OnUpdateDropdown, Dispatcher);
			m_updateTimer.Start();

			AvailableDeviceDropdown.SelectionChanged += OnSelectedDeviceChanged;
		}

		public void SetApiController(BlackmagicBluetoothCameraAPIController a_controller)
		{
			m_cameraApiController = a_controller;
			m_cameraApiController.OnCameraRequestPairingCode = OnCameraRequestedPairingCode;
		}

		private void OnUpdateDropdown(object? a_sender, EventArgs a_e)
		{
			if (m_cameraApiController == null)
			{
				return;
			}

			foreach (BlackmagicBluetoothCameraAPIController.AdvertisementEntry entry in
			         m_cameraApiController.GetAdvertisedDevices())
			{
				AdvertisedDeviceEntry dropDownEntry = new AdvertisedDeviceEntry(entry.DeviceBluetoothAddress, entry.DeviceShortName);
				if (!AvailableDeviceDropdown.Items.Contains(dropDownEntry))
				{
					AvailableDeviceDropdown.Items.Add(dropDownEntry);
				}
			}
		}

		private void OnSelectedDeviceChanged(object a_sender, SelectionChangedEventArgs a_e)
		{
			ulong selectedDeviceAddress = ((AdvertisedDeviceEntry) AvailableDeviceDropdown.SelectedItem).DeviceId;
			OnBluetoothDeviceSelected.Invoke(selectedDeviceAddress);
			throw new NotImplementedException();
			//	m_cameraApiController?.AsyncTryConnectToDevice(selectedDeviceAddress);
		}

		private string? OnCameraRequestedPairingCode(string a_cameraDisplayName)
		{
			string pairingCode = "";
			Dispatcher.Invoke(() => { 
				ProvideBluetoothPairingCodeWindow window = new ProvideBluetoothPairingCodeWindow(a_cameraDisplayName);
				window.ShowDialog();
				pairingCode = window.PairingCode;
			});

			return pairingCode;
		}
	}
}
