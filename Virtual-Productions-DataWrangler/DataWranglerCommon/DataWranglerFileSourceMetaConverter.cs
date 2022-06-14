using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataWranglerCommon;

public class DataWranglerFileSourceMetaConverter : JsonConverter<DataWranglerFileSourceMeta>
{
	public override bool CanWrite => false;

	public override void WriteJson(JsonWriter writer, DataWranglerFileSourceMeta? value, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}

	public override DataWranglerFileSourceMeta? ReadJson(JsonReader reader, Type objectType, DataWranglerFileSourceMeta? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		JObject? rootObject = serializer.Deserialize<JObject>(reader);
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