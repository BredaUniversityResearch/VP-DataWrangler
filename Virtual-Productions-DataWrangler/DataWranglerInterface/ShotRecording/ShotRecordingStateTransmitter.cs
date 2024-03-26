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
	}

	public void Dispose()
	{
		DataWranglerServiceProvider.Instance.NetworkDeviceAPI.UnregisterEventHandler(this);

		m_state.OnSelectedProjectChanged -= OnProjectChanged;
		m_state.OnSelectedShotChanged -= OnShotChanged;
		m_state.OnSelectedShotVersionChanged -= OnShotVersionChanged;
	}

	private void OnProjectChanged(DataEntityProject? a_obj)
	{
		throw new NotImplementedException();
	}

	private void OnShotChanged(DataEntityShot? a_obj)
	{
		throw new NotImplementedException();
	}

	private void OnShotVersionChanged(DataEntityShotVersion? a_obj)
	{
		throw new NotImplementedException();
	}

	public void OnClientConnected(int a_connectionId, NetworkAPIDeviceCapabilities a_capabilities)
	{
		//throw new NotImplementedException();
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