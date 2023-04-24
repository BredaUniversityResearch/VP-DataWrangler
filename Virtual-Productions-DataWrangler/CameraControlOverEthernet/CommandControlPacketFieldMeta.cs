using System.Reflection;
using System.Text;

namespace CameraControlOverEthernet;

internal class CommandControlPacketFieldMeta
{
	enum EDataType
	{
		Int32,
		UInt32,
		String,
		ByteArray
	};

	private EDataType m_dataType;
	private FieldInfo m_targetField;

	public CommandControlPacketFieldMeta(FieldInfo a_info)
	{
		m_targetField = a_info;
		m_dataType = FieldTypeToDefaultDataType(a_info.FieldType);
	}

	private EDataType FieldTypeToDefaultDataType(Type a_fieldType)
	{
		if (a_fieldType == typeof(int))
		{
			return EDataType.Int32;
		}
		else if (a_fieldType == typeof(uint))
		{
			return EDataType.UInt32;
		}
		else if (a_fieldType == typeof(string))
		{
			return EDataType.String;
		}
		else if (a_fieldType == typeof(byte[]))
		{
			return EDataType.ByteArray;
		}

		throw new Exception($"Cannot convert type {a_fieldType.Name} to primitive");
	}

	public void Write(BinaryWriter a_writer, object a_instance)
	{
		switch (m_dataType)
		{
			case EDataType.Int32:
				a_writer.Write((int) m_targetField.GetValue(a_instance)!);
				break;
			case EDataType.UInt32:
				a_writer.Write((uint) m_targetField.GetValue(a_instance)!);
				break;
			case EDataType.String:
				string strVal = (string) m_targetField.GetValue(a_instance)!;
				a_writer.Write((ushort) strVal.Length);
				a_writer.Write(Encoding.ASCII.GetBytes(strVal));
				break;
			case EDataType.ByteArray:
			{
				byte[] value = (byte[])m_targetField.GetValue(a_instance)!;
				a_writer.Write((ushort)value.Length);
				a_writer.Write(value);
				break;
			}
			default:
				throw new Exception($"Unimplemented primitive type {m_dataType}");
		}
	}

	public void Read(BinaryReader a_reader, object a_target)
	{
		switch (m_dataType)
		{
			case EDataType.Int32:
				m_targetField.SetValue(a_target, a_reader.ReadInt32());
				break;
			case EDataType.UInt32:
				m_targetField.SetValue(a_target, a_reader.ReadUInt32());
				break;
			case EDataType.String:
				ushort stringLength = a_reader.ReadUInt16();
				byte[] stringBytes = a_reader.ReadBytes(stringLength);
				m_targetField.SetValue(a_target, Encoding.ASCII.GetString(stringBytes));
				break;
			case EDataType.ByteArray:
			{
				ushort arrayLength = a_reader.ReadUInt16();
				byte[] value = a_reader.ReadBytes(arrayLength);
				if (value.Length != arrayLength)
				{
					throw new EndOfStreamException("Failed to read required amount of data");
				}

				m_targetField.SetValue(a_target, value);
				break;
			}
			default:
				throw new Exception($"Reading unimplemented primitive {m_dataType}");
		}
	}
};