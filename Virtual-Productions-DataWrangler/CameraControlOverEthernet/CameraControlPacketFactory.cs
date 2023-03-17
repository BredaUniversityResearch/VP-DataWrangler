using System.Diagnostics;
using System.Reflection;

namespace CameraControlOverEthernet
{
	internal class CameraControlPacketFactory
	{
		private static Dictionary<uint, CommandControlPacketMeta> ms_knownPackets = new Dictionary<uint, CommandControlPacketMeta>();

		static CameraControlPacketFactory()
		{
			Assembly? activeAssembly = Assembly.GetAssembly(typeof(ICameraControlPacket));
			Debug.Assert(activeAssembly != null, "activeAssembly != null");
			foreach (Type type in activeAssembly.GetTypes())
			{
				if (type.IsAssignableTo(typeof(ICameraControlPacket)))
				{
					uint packetIdentifier = DJB2aHasher.Hash(type.Name);
					ms_knownPackets.Add(packetIdentifier, new CommandControlPacketMeta(type));
				}
			}
		}

		public static CommandControlPacketMeta GetMeta(ICameraControlPacket a_packet)
		{
			uint identifier = CommandControlPacketMeta.GetIdentifierForType(a_packet.GetType());
			CommandControlPacketMeta? meta = FindMeta(identifier);
			if (meta == null)
			{
				throw new Exception($"Could not find type {a_packet.GetType()} in internal type registry");
			}

			return meta;
		}

		public static CommandControlPacketMeta? FindMeta(uint a_packetIdentifier)
		{
			if (ms_knownPackets.TryGetValue(a_packetIdentifier, out CommandControlPacketMeta? meta))
			{
				return meta;
			}

			return null;
		}
	}
}
