using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataWranglerCommon;

public class DataWranglerFileSourceMetaConverter : JsonConverter<DataWranglerFileSourceMeta>
{
	public override bool CanWrite => false;

	public override void WriteJson(JsonWriter a_writer, DataWranglerFileSourceMeta? a_value, JsonSerializer a_serializer)
	{
		throw new NotImplementedException();
	}

	public override DataWranglerFileSourceMeta? ReadJson(JsonReader a_reader, Type a_objectType, DataWranglerFileSourceMeta? a_existingValue, bool a_hasExistingValue, JsonSerializer a_serializer)
	{
		JObject? rootObject = a_serializer.Deserialize<JObject>(a_reader);
		if (rootObject == null)
		{
			throw new JsonSerializationException("Failed to deserialize object to JObject (DataWranglerFileSourceMeta)");
		}

		if (rootObject.TryGetValue("SourceType", out JToken? sourceTypeToken))
		{
			string sourceTypeName = sourceTypeToken.ToString();
			DataWranglerFileSourceMeta meta = DataWranglerFileSourceMeta.CreateFromTypeName(sourceTypeName);
			using (var sr = rootObject.CreateReader())
			{
				JsonSerializer.CreateDefault().Populate(sr, meta); // Uses the system default JsonSerializerSettings
			}

			return meta;
		}

		return null;
	}
}