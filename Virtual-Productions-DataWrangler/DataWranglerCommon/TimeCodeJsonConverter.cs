using Newtonsoft.Json;

namespace DataWranglerCommon
{
	public class TimeCodeJsonConverter: JsonConverter<TimeCode>
	{
		public override void WriteJson(JsonWriter writer, TimeCode value, JsonSerializer serializer)
		{
			serializer.Serialize(writer, value.ToString());
		}

		public override TimeCode ReadJson(JsonReader reader, Type objectType, TimeCode existingValue, bool hasExistingValue,
			JsonSerializer serializer)
		{
			string? value = serializer.Deserialize<string>(reader);
			if (value == null)
			{
				throw new Exception($"Failed to deserialize TimeCode from value {reader.Value}");
			}
			return TimeCode.FromString(value);
		}
	}
}
