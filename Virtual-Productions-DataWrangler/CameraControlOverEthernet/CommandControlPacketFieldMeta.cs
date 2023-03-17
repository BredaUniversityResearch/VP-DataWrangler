using System.Reflection;

namespace CameraControlOverEthernet;

internal class CommandControlPacketFieldMeta
{
	enum EPrimitiveType
	{
		Int32,
		UInt32
	};

	private EPrimitiveType m_primitiveType;
	private FieldInfo m_targetField;
	public int SerializedLength { get; private set; }

	public CommandControlPacketFieldMeta(FieldInfo a_info)
	{
		m_targetField = a_info;
		m_primitiveType = FieldTypeToPrimitive(a_info.FieldType);
	}


	private EPrimitiveType FieldTypeToPrimitive(Type a_fieldType)
	{
		if (a_fieldType == typeof(int))
		{
			SerializedLength = sizeof(int);
			return EPrimitiveType.Int32;
		}
		else if (a_fieldType == typeof(uint))
		{
			SerializedLength = sizeof(uint);
			return EPrimitiveType.UInt32;
		}

		throw new Exception($"Cannot convert type {a_fieldType.Name} to primitive");
	}

	public void Write(BinaryWriter a_writer, object a_instance)
	{
		switch (m_primitiveType)
		{
			case EPrimitiveType.Int32:
				a_writer.Write((int) m_targetField.GetValue(a_instance)!);
				break;
			case EPrimitiveType.UInt32:
				a_writer.Write((uint) m_targetField.GetValue(a_instance)!);
				break;
			default:
				throw new Exception($"Unimplemented primitive type {m_primitiveType}");
		}
	}

	public void Read(BinaryReader a_reader, object a_target)
	{
		switch (m_primitiveType)
		{
			case EPrimitiveType.Int32:
				m_targetField.SetValue(a_target, a_reader.ReadInt32());
				break;
			case EPrimitiveType.UInt32:
				m_targetField.SetValue(a_target, a_reader.ReadUInt32());
				break;
			default:
				throw new Exception($"Reading unimplemented primitive {m_primitiveType}");
		}
	}
};