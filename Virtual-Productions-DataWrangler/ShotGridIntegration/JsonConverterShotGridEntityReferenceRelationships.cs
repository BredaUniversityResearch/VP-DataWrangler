using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ShotGridIntegration
{
	public class JsonConverterShotGridEntityReferenceRelationships : JsonConverter<ShotGridEntityReference>
	{
		public override bool CanWrite => false;

		public override void WriteJson(JsonWriter writer, ShotGridEntityReference? value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}

		public override ShotGridEntityReference? ReadJson(JsonReader reader, Type objectType, ShotGridEntityReference? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			JObject? withMetaData = serializer.Deserialize<JObject>(reader);
			if (withMetaData == null)
			{
				throw new JsonException("Failed to read object from value");
			}

			if (withMetaData.TryGetValue("data", out JToken? dataNode))
			{
				return dataNode.ToObject<ShotGridEntityReference>(serializer);
			}

			throw new Exception("Missing data node on entity relationships relation");

			return null;
		}
	}
}
