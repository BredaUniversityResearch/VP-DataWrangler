using System.Reflection;

namespace BlackmagicCameraControlData.CommandPackets
{
	public readonly struct CommandIdentifier : IEquatable<CommandIdentifier>
	{
		public readonly byte Category;
		public readonly byte Parameter;

		public CommandIdentifier(byte a_category, byte a_parameter)
		{
			Category = a_category;
			Parameter = a_parameter;
		}

		public override int GetHashCode()
		{
			return Category | (Parameter >> 8);
		}

		public override string ToString()
		{
			return $"[Command: {Category}:{Parameter}]";
		}

		public bool Equals(CommandIdentifier other)
		{
			return Category == other.Category && Parameter == other.Parameter;
		}

		public override bool Equals(object? obj)
		{
			return obj is CommandIdentifier other && Equals(other);
		}

		public static bool operator ==(CommandIdentifier left, CommandIdentifier right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(CommandIdentifier left, CommandIdentifier right)
		{
			return !left.Equals(right);
		}

		public static CommandIdentifier FromInstance(ICommandPacketBase a_packet)
		{
			CommandPacketMetaAttribute? attribute = a_packet.GetType().GetCustomAttribute<CommandPacketMetaAttribute>(false);
			if (attribute == null)
			{
				throw new Exception($"Failed to get the CommandPacketMetaAttribute from type {a_packet.GetType().Name}");
			}

			return attribute.Identifier;
		}
	}
}
