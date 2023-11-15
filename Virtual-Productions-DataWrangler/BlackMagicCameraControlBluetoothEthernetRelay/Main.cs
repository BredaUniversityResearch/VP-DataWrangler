using BlackMagicCameraControlBluetoothEthernetRelay;
using CameraControlOverEthernet;
using CommonLogging;

class App
{
	//private readonly CameraControlNetworkReceiver m_networkReceiver = new CameraControlNetworkReceiver();
	private CameraControlBluetoothRelay? m_relay; 

	private void Run()
	{
		m_relay = new CameraControlBluetoothRelay();

		//m_networkReceiver.Start();

		while (m_relay.ShouldKeepRunning)
		{
			m_relay.Update();
		}
	}

	public static void Main()
	{
		Logger.Instance.OnMessageLogged += LogToConsole;

		App application = new App();
		application.Run();
	}

	private static void LogToConsole(TimeOnly a_time, string a_source, ELogSeverity a_severity, string a_message)
	{
		Console.WriteLine($"{a_time}\t{a_severity}\t{a_source}\t{a_message}");
	}
}

