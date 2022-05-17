using System.Windows;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for ProvideBluetoothPairingCodeWindow.xaml
	/// </summary>
	public partial class ProvideBluetoothPairingCodeWindow : Window
	{
		public string PairingCode => PairingCodeInput.Text;

		public ProvideBluetoothPairingCodeWindow(string a_deviceName)
		{
			InitializeComponent();

			DeviceName.Content = a_deviceName;
			SubmitButton.Click += OnSubmit;
		}

		private void OnSubmit(object a_sender, RoutedEventArgs a_e)
		{
			if (PairingCodeInput.Text.Length > 0)
			{
				Close();
			}
		}
	}
}
