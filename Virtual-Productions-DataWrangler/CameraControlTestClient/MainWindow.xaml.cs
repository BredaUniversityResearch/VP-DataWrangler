using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using BlackmagicCameraControlData;
using BlackmagicCameraControlData.CommandPackets;
using CameraControlOverEthernet;
using CameraControlOverEthernet.CameraControl;
using CommonLogging;

namespace CameraControlTestClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private CancellationTokenSource m_backgroundUpdateCancellationToken = new CancellationTokenSource();

        private readonly CameraDeviceHandle m_emulatedDeviceHandle = new CameraDeviceHandle($"Emulated_Camera_{Random.Shared.Next()}", new CameraControllerBase());
        private readonly NetworkedDeviceAPIClient m_apiClient;

        private bool m_isCurrentlyRecording = false;

        public MainWindow()
        {
            InitializeComponent();

            Logger.Instance.OnMessageLogged += OnMessageLogged;

            m_apiClient = new NetworkedDeviceAPIClient(NetworkAPIDeviceCapabilities.EDeviceRole.CameraRelay);

            m_apiClient.OnConnected += ClientOnConnected;
            m_apiClient.OnDisconnected += ClientOnDisconnected;
            m_apiClient.OnDataReceived += ClientOnDataReceived;

            m_apiClient.StartListenForServer();

            Task.Run(() =>
            {
                while (!m_backgroundUpdateCancellationToken.IsCancellationRequested)
                {
                    if (m_apiClient.IsConnected)
                    {
                        TimeCode currentTimeCode = TimeCode.FromTime(TimeOnly.FromDateTime(DateTime.Now), 25);
                        MemoryStream ms = new MemoryStream(128);
                        CommandWriter writer = new CommandWriter(ms);
                        CommandPacketSystemTimeCode timeCodePacket = new CommandPacketSystemTimeCode(currentTimeCode);
                        writer.WriteCommand(timeCodePacket);
                        m_apiClient.SendPacket(new CameraControlDataPacket(m_emulatedDeviceHandle, currentTimeCode, ms.ToArray()));
                    }

                    Thread.Sleep(100);
                }
            }, m_backgroundUpdateCancellationToken.Token);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            m_backgroundUpdateCancellationToken.Cancel();
        }

        private static void OnMessageLogged(TimeOnly a_time, string a_source, ELogSeverity a_severity, string a_message)
        {
            Console.WriteLine($"[{a_source}]: {a_message}");
        }

        private void ClientOnConnected(int a_serverIdentifier)
        {
            Console.WriteLine($"Connected to server with identifier {a_serverIdentifier}");

            m_apiClient.SendPacket(new CameraControlCameraConnectedPacket(m_emulatedDeviceHandle));
        }

        private void ClientOnDisconnected(int a_serverIdentifier)
        {
            m_apiClient.SendPacket(new CameraControlCameraDisconnectedPacket(m_emulatedDeviceHandle));

            Console.WriteLine($"Disconnected from server with identifier {a_serverIdentifier}");
        }

        private static void ClientOnDataReceived(INetworkAPIPacket a_packet, int a_serverIdentifier)
        {
            Console.WriteLine($"Data packet received {a_packet.GetType()}");
        }

        private void ToggleRecordingButtonClick(object a_sender, RoutedEventArgs a_e)
        {
            m_isCurrentlyRecording = !m_isCurrentlyRecording;

            SendCommandMessage(new CommandPacketMediaTransportMode() { 
                Flags = CommandPacketMediaTransportMode.EFlags.Disk1Active, 
                Mode = (m_isCurrentlyRecording)? CommandPacketMediaTransportMode.EMode.Record : CommandPacketMediaTransportMode.EMode.Preview, 
                Slot1StorageMedium = CommandPacketMediaTransportMode.EStorageMedium.SSD, 
                Slot2StorageMedium = CommandPacketMediaTransportMode.EStorageMedium.CFast, 
                Speed = 1
            });
            ToggleRecordingButton.Content = (m_isCurrentlyRecording)? "Stop Recording" : "Start Recording";
        }

        private void SendCommandMessage(ICommandPacketBase a_packet)
        {
            TimeCode currentTimeCode = TimeCode.FromTime(TimeOnly.FromDateTime(DateTime.Now), 25);
            MemoryStream ms = new MemoryStream(128);
            CommandWriter writer = new CommandWriter(ms);
            writer.WriteCommand(a_packet);
            m_apiClient.SendPacket(new CameraControlDataPacket(m_emulatedDeviceHandle, currentTimeCode, ms.ToArray()));
        }
    }
}
