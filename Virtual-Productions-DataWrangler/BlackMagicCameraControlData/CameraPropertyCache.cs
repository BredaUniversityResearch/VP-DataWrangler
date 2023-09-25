using BlackmagicCameraControlData.CommandPackets;
using DataWranglerCommon;

namespace BlackmagicCameraControlData
{
	//Caches all camera properties, and dispatches event when data has changed.
	public class CameraPropertyCache
	{
		public class CacheEntry
		{
			public TimeCode LastUpdateTime;
			public ICommandPacketBase PacketData;

			public CacheEntry(TimeCode a_lastUpdateTime, ICommandPacketBase a_packetData)
			{
				LastUpdateTime = a_lastUpdateTime;
				PacketData = a_packetData;
			}
		};

		private readonly Dictionary<CommandIdentifier, CacheEntry> m_currentValues = new Dictionary<CommandIdentifier, CacheEntry>();
		public IReadOnlyDictionary<CommandIdentifier, CacheEntry> CurrentValues => m_currentValues;

		public bool CheckPropertyChanged(ICommandPacketBase a_packet, TimeCode a_packetTimeCode)
		{
			return CheckPropertyChanged(CommandIdentifier.FromInstance(a_packet), a_packet, a_packetTimeCode);
		}

		public bool CheckPropertyChanged(CommandIdentifier a_identifier, ICommandPacketBase a_packet, TimeCode a_packetTimeCode)
		{
			bool wasChanged = false;
			if (m_currentValues.TryGetValue(a_identifier, out CacheEntry? existingValue))
			{
				//We can have packets that are 'updated' less recently than we had earlier, for example in the case of playback.
				if (!existingValue.PacketData.Equals(a_packet))
				{
					existingValue.PacketData = a_packet;
					existingValue.LastUpdateTime = a_packetTimeCode;
					wasChanged = true;
				}
			}
			else
			{
				m_currentValues.Add(a_identifier, new CacheEntry(a_packetTimeCode, a_packet));
				wasChanged = true;
			}

			return wasChanged;
		}
	}
}
