using CameraControlOverEthernet;
using CommonLogging;

class App
{
	private CameraControlServer m_server = new CameraControlServer();
	private CameraControlClient m_client = new CameraControlClient();

	private void Run()
	{
		m_server.Start();
		m_client.StartListenForServer();

		while (true)
			continue;
	}

	public static void Main()
	{
		Logger.Instance.OnMessageLogged += LogToConsole;

		App application = new App();
		application.Run();
	}

	private static void LogToConsole(string a_source, ELogSeverity a_severity, string a_message)
	{
		Console.WriteLine($"{a_severity}\t{a_source}\t{a_message}");
	}
}

