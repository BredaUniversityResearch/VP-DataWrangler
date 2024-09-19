using System.ComponentModel;
using CameraControlOverEthernet;
using DataApiCommon;

namespace DataWranglerInterface.ShotRecording;

internal class ShotRecordingStateTransmitter: IDisposable, INetworkAPIEventHandler
{
	private ShotRecordingApplicationState m_state;

	public ShotRecordingStateTransmitter(ShotRecordingApplicationState a_applicationState)
	{
		DataWranglerServiceProvider.Instance.NetworkDeviceAPI.RegisterEventHandler(this);

		m_state = a_applicationState;
		m_state.PropertyChanged += OnRecordingStagePropertyChanged;
	}

	public void Dispose()
	{
		DataWranglerServiceProvider.Instance.NetworkDeviceAPI.UnregisterEventHandler(this);

		m_state.PropertyChanged -= OnRecordingStagePropertyChanged;
	}

	private ShotRecordingStatePacket CreateUpdatedRecordingStatePacket()
	{
		return new ShotRecordingStatePacket(m_state.SelectedProject?.Name ?? "",
			m_state.SelectedShot?.ShotName ?? "", m_state.SelectedShotVersion?.ShotVersionName ?? "", 
			m_state.NextPredictedShotVersionName);
	}

	private void OnRecordingStagePropertyChanged(object? a_sender, PropertyChangedEventArgs a_e)
	{
		var transmitter = DataWranglerServiceProvider.Instance.NetworkDeviceAPI;
		transmitter.SendMessageToAllConnectedClients(CreateUpdatedRecordingStatePacket()); 
	}

	public void OnClientConnected(int a_connectionId, NetworkAPIDeviceCapabilities a_capabilities)
	{
		var transmitter = DataWranglerServiceProvider.Instance.NetworkDeviceAPI;
		transmitter.SendMessageToConnection(a_connectionId, CreateUpdatedRecordingStatePacket());
	}

	public void OnClientDisconnected(int a_connectionId)
	{
		//throw new NotImplementedException();
	}

	public void OnPacketReceived(INetworkAPIPacket a_packet, int a_connectionId)
	{
		//throw new NotImplementedException();
	}
}