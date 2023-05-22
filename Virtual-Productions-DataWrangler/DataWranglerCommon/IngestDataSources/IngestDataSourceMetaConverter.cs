using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataWranglerCommon.IngestDataSources;

public class IngestDataSourceMetaConverter : JsonConverter<IngestDataSourceMeta>
{
	private static Dictionary<string, Type> ms_dataSourceMetaNames = new Dictionary<string, Type>();

	static IngestDataSourceMetaConverter()
	{
		foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			foreach (Type type in assembly.GetTypes())
			{
				if (!type.IsAbstract && type.IsAssignableTo(typeof(IngestDataSourceMeta)))
				{
					IngestDataSourceMeta meta = (IngestDataSourceMeta) RuntimeHelpers.GetUninitializedObject(type);
					ms_dataSourceMetaNames.Add(meta.SourceType, type);
				}
			}
		}
	}

	public override bool CanWrite => false;

	public override void WriteJson(JsonWriter a_writer, IngestDataSourceMeta? a_value, JsonSerializer a_serializer)
	{
		throw new NotImplementedException();
	}

	public override IngestDataSourceMeta? ReadJson(JsonReader a_reader, Type a_objectType, IngestDataSourceMeta? a_existingValue, bool a_hasExistingValue, JsonSerializer a_serializer)
	{
		JObject? rootObject = a_serializer.Deserialize<JObject>(a_reader);
		if (rootObject == null)
		{
			throw new JsonSerializationException("Failed to deserialize object to JObject (IngestDataSourceMeta)");
		}

		if (rootObject.TryGetValue("SourceType", out JToken? sourceTypeToken))
		{
			string sourceTypeName = sourceTypeToken.ToString();
			IngestDataSourceMeta? meta = TryCreateMetaFromName(sourceTypeName);
			if (meta != null)
			{
				using (var sr = rootObject.CreateReader())
				{
					JsonSerializer.CreateDefault(DataWranglerSerializationSettings.Instance).Populate(sr, meta);
				}
			}
			else
			{
				throw new JsonSerializationException($"Failed to create IngestDataSourceMeta. Unknown type of IngestMeta: {sourceTypeName}");
			}

			return meta;
		}

		throw new JsonSerializationException($"Failed to populate IngestDataSourceMeta. Required field \"SourceType\" missing. Full Json: {rootObject.ToString()}");
	}

	private IngestDataSourceMeta? TryCreateMetaFromName(string a_sourceTypeName)
	{
		if (ms_dataSourceMetaNames.TryGetValue(a_sourceTypeName, out Type? targetType))
		{
			return (IngestDataSourceMeta) RuntimeHelpers.GetUninitializedObject(targetType);
		}

		return null;
	}
}