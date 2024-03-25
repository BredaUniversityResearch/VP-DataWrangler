using System.Reflection;
using System.Runtime.CompilerServices;

namespace CameraControlOverEthernet;

internal class NetworkAPIPacketMeta
{
	private readonly Type m_targetType;
	private readonly List<NetworkAPIPacketFieldMeta> m_fieldMeta = new List<NetworkAPIPacketFieldMeta>();
	public IReadOnlyCollection<NetworkAPIPacketFieldMeta> Fields => m_fieldMeta;

	public readonly uint Identifier;

	public NetworkAPIPacketMeta(Type a_type)
	{
		m_targetType = a_type;
		Identifier = GetIdentifierForType(a_type);
		foreach (FieldInfo fi in a_type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
		{
			NetworkAPIPacketFieldMeta meta = new NetworkAPIPacketFieldMeta(fi);
			m_fieldMeta.Add(meta);
		}
	}

	public static uint GetIdentifierForType(Type a_type)
	{
		return DJB2aHasher.Hash(a_type.Name);
	}

	public INetworkAPIPacket CreateDefaultedInstance()
	{
		return (INetworkAPIPacket)RuntimeHelpers.GetUninitializedObject(m_targetType);
	}
};