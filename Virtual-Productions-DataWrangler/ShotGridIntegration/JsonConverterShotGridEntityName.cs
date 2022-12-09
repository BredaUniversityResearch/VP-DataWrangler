using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ShotGridIntegration
{
	public class JsonConverterShotGridEntityName: JsonConverter<ShotGridEntityName>
	{
		public override void WriteJson(JsonWriter writer, ShotGridEntityName? value, JsonSerializer serializer)
		{
			serializer.Serialize(writer, value?.CamelCase);
		}

		public override ShotGridEntityName? ReadJson(JsonReader reader, Type objectType, ShotGridEntityName? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			string? typeName = serializer.Deserialize<string>(reader);
			if (typeName == null)
			{
				throw new JsonException("Failed to read string from value");
			}

			return ShotGridEntityName.FromCamelCaseName(typeName);
		}
	}
}
