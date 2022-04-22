using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace ShotGridIntegration
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	[JsonConverter(typeof(ShotGridSimpleSearchFilterConverter))]
	public class ShotGridSimpleSearchFilter
	{
		public readonly List<ShotGridSearchCondition> conditions = new List<ShotGridSearchCondition>();

		public void FieldIs(string a_field, string a_status)
		{
			conditions.Add(new ShotGridSearchCondition(a_field, "is", a_status));
		}
	}

	internal class ShotGridSimpleSearchFilterConverter : JsonConverter<ShotGridSimpleSearchFilter>
	{
		public override void WriteJson(JsonWriter writer, ShotGridSimpleSearchFilter? value, JsonSerializer serializer)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			serializer.Serialize(writer, value.conditions);
		}

		public override ShotGridSimpleSearchFilter? ReadJson(JsonReader reader, Type objectType,
			ShotGridSimpleSearchFilter? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}
	}
}
