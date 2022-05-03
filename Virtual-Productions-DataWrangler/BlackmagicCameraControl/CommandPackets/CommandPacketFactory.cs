using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace BlackmagicCameraControl.CommandPackets
{
	internal static class CommandPacketFactory
	{
		private static Dictionary<CommandIdentifier, CommandMeta> ms_knownCommands = new Dictionary<CommandIdentifier, CommandMeta>();

		static CommandPacketFactory()
		{
			Assembly? activeAssembly = Assembly.GetAssembly(typeof(ICommandPacketBase));
			Debug.Assert(activeAssembly != null, "activeAssembly != null");
			foreach (Type type in activeAssembly.GetTypes())
			{
				CommandPacketMetaAttribute? metaAttribute = type.GetCustomAttribute<CommandPacketMetaAttribute>();
				if (metaAttribute != null)
				{
					Debug.Assert(type.GetConstructor(new [] {typeof(CommandReader)}) != null, $"Failed to find public constructor with CommandReader as single param on type {type.Name}");
					ms_knownCommands.Add(metaAttribute.Identifier, new CommandMeta(type, metaAttribute.Identifier, metaAttribute.SerializedSizeBytes, metaAttribute.DataType));
				}
			}
		}

		public static ICommandPacketBase? CreatePacket(CommandIdentifier a_identifier, CommandReader a_reader)
		{
			if (ms_knownCommands.TryGetValue(a_identifier, out CommandMeta? target))
			{
				return (ICommandPacketBase?)Activator.CreateInstance(target.CommandType, a_reader);
			}

			return null;
		}

		public static CommandMeta? FindCommandMeta(CommandIdentifier a_identifier)
		{
			if (ms_knownCommands.TryGetValue(a_identifier, out CommandMeta? target))
			{
				return target;
			}
			return null;
		}

		public static CommandMeta? FindCommandMeta(Type a_type)
		{
			foreach(var kvp in ms_knownCommands)
			{
				if (kvp.Value.CommandType == a_type)
				{
					return kvp.Value;
				}
			}

			return null;
		}
	}
}
