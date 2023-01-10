using BlackmagicCameraControl.CommandPackets;
using BlackmagicCameraControlData.CommandPackets;

namespace BlackmagicDeckLinkControl
{
	//Caches all camera properties, and dispatches event when data has changed.
	public class CameraPropertyCache
	{
		private readonly Dictionary<CommandIdentifier, ICommandPacketBase> m_currentValues = new Dictionary<CommandIdentifier, ICommandPacketBase>();

		public delegate void CameraPropertyChanged(CommandIdentifier a_identifier, ICommandPacketBase a_data);
		public event CameraPropertyChanged? OnCameraPropertyChanged;

		public void OnPropertyReceived(CommandIdentifier a_identifier, ICommandPacketBase a_packet)
		{
			if (m_currentValues.TryGetValue(a_identifier, out ICommandPacketBase? existingValue))
			{
				if (existingValue != a_packet)
				{
					m_currentValues[a_identifier] = a_packet;
					OnCameraPropertyChanged?.Invoke(a_identifier, a_packet);
				}
			}
			else
			{
				m_currentValues.Add(a_identifier, a_packet);
				OnCameraPropertyChanged?.Invoke(a_identifier, a_packet);
			}
		}
	}
}
