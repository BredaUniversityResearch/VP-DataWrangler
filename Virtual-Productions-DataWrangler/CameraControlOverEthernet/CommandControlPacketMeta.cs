using System.Reflection;
using System.Runtime.CompilerServices;

namespace CameraControlOverEthernet;

internal class CommandControlPacketMeta
{
	private readonly Type m_targetType;
	private readonly List<CommandControlPacketFieldMeta> m_fieldMeta = new List<CommandControlPacketFieldMeta>();
	public IReadOnlyCollection<CommandControlPacketFieldMeta> Fields => m_fieldMeta;

	public readonly uint Identifier;

	public CommandControlPacketMeta(Type a_type)
	{
		m_targetType = a_type;
		Identifier = GetIdentifierForType(a_type);
		foreach (FieldInfo fi in a_type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
		{
			CommandControlPacketFieldMeta meta = new CommandControlPacketFieldMeta(fi);
			m_fieldMeta.Add(meta);
		}
	}

	public static uint GetIdentifierForType(Type a_type)
	{
		return DJB2aHasher.Hash(a_type.Name);
	}

	public ICameraControlPacket CreateDefaultedInstance()
	{
		return (ICameraControlPacket)RuntimeHelpers.GetUninitializedObject(m_targetType);
	}
};