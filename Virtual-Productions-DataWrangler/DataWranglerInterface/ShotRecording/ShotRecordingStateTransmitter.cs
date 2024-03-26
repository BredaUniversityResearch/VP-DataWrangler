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
		m_state.OnSelectedProjectChanged += OnProjectChanged;
		m_state.OnSelectedShotChanged += OnShotChanged;
		m_state.OnSelectedShotVersionChanged += OnShotVersionChanged;
		m_state.OnPredictedNextShotVersionNameChanged += OnPredictedShotVersionNameChanged;
	}

	public void Dispose()
	{
		DataWranglerServiceProvider.Instance.NetworkDeviceAPI.UnregisterEventHandler(this);

		m_state.OnSelectedProjectChanged -= OnProjectChanged;
		m_state.OnSelectedShotChanged -= OnShotChanged;
		m_state.OnSelectedShotVersionChanged -= OnShotVersionChanged;
		m_state.OnPredictedNextShotVersionNameChanged -= OnPredictedShotVersionNameChanged;
	}

	private ShotRecordingStatePacket CreateUpdatedRecordingStatePacket()
	{
		return new ShotRecordingStatePacket(m_state.SelectedProject?.Name ?? "",
			m_state.SelectedShot?.ShotName ?? "", m_state.SelectedShotVersion?.ShotVersionName ?? "", 
			m_state.NextPredictedShotVersionName);
	}

	private void OnProjectChanged(DataEntityProject? a_obj)
	{
		var transmitter = DataWranglerServiceProvider.Instance.NetworkDeviceAPI;
		transmitter.SendMessageToAllConnectedClients(CreateUpdatedRecordingStatePacket()); 
	}

	private void OnShotChanged(DataEntityShot? a_obj)
	{
		var transmitter = DataWranglerServiceProvider.Instance.NetworkDeviceAPI;
		transmitter.SendMessageToAllConnectedClients(CreateUpdatedRecordingStatePacket()); 
	}

	private void OnShotVersionChanged(DataEntityShotVersion? a_obj)
	{
		var transmitter = DataWranglerServiceProvider.Instance.NetworkDeviceAPI;
		transmitter.SendMessageToAllConnectedClients(CreateUpdatedRecordingStatePacket()); 
	}

	private void OnPredictedShotVersionNameChanged(string a_obj)
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