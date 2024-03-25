using System.Diagnostics;
using System.Reflection;

namespace CameraControlOverEthernet
{
	internal class NetworkAPIPacketFactory
	{
		private static Dictionary<uint, NetworkAPIPacketMeta> ms_knownPackets = new Dictionary<uint, NetworkAPIPacketMeta>();

		static NetworkAPIPacketFactory()
		{
			Assembly? activeAssembly = Assembly.GetAssembly(typeof(INetworkAPIPacket));
			Debug.Assert(activeAssembly != null, "activeAssembly != null");
			foreach (Type type in activeAssembly.GetTypes())
			{
				if (type.IsAssignableTo(typeof(INetworkAPIPacket)))
				{
					uint packetIdentifier = DJB2aHasher.Hash(type.Name);
					ms_knownPackets.Add(packetIdentifier, new NetworkAPIPacketMeta(type));
				}
			}
		}

		public static NetworkAPIPacketMeta GetMeta(INetworkAPIPacket a_packet)
		{
			uint identifier = NetworkAPIPacketMeta.GetIdentifierForType(a_packet.GetType());
			NetworkAPIPacketMeta? meta = FindMeta(identifier);
			if (meta == null)
			{
				throw new Exception($"Could not find type {a_packet.GetType()} in internal type registry");
			}

			return meta;
		}

		public static NetworkAPIPacketMeta? FindMeta(uint a_packetIdentifier)
		{
			if (ms_knownPackets.TryGetValue(a_packetIdentifier, out NetworkAPIPacketMeta? meta))
			{
				return meta;
			}

			return null;
		}
	}
}
