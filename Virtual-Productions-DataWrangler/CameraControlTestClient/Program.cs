using CameraControlOverEthernet;
using CommonLogging;

namespace CameraControlTestClient
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Logger.Instance.OnMessageLogged += OnMessageLogged;

            NetworkedDeviceAPIClient client = new NetworkedDeviceAPIClient();

            client.OnConnected += ClientOnConnected;
            client.OnDisconnected += ClientOnDisconnected;

            client.StartListenForServer();
            while (true)
            {
                client.Update();
                Thread.Sleep(100);
            }
        }

        private static void OnMessageLogged(TimeOnly a_time, string a_source, ELogSeverity a_severity, string a_message)
        {
            Console.WriteLine($"[{a_source}]: {a_message}");
        }

        private static void ClientOnConnected(int a_serverIdentifier)
        {
            Console.WriteLine($"Connected to server with identifier {a_serverIdentifier}");
        }

        private static void ClientOnDisconnected(int a_serverIdentifier)
        {
            Console.WriteLine($"Disconnected from server with identifier {a_serverIdentifier}");
        }
    }
}
