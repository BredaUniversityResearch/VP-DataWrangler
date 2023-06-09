using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ShotGridIntegration
{
	public class JsonConverterShotGridEntityName: JsonConverter<ShotGridEntityTypeInfo>
	{
		public override void WriteJson(JsonWriter writer, ShotGridEntityTypeInfo? value, JsonSerializer serializer)
		{
			serializer.Serialize(writer, value?.CamelCase);
		}

		public override ShotGridEntityTypeInfo? ReadJson(JsonReader reader, Type objectType, ShotGridEntityTypeInfo? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			string? typeName = serializer.Deserialize<string>(reader);
			if (typeName == null)
			{
				throw new JsonException("Failed to read string from value");
			}

			return ShotGridEntityTypeInfo.FromCamelCaseName(typeName);
		}
	}
}
