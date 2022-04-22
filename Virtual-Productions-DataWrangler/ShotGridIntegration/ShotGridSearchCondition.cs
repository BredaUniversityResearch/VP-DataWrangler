using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ShotGridIntegration
{
	[JsonConverter(typeof(ShotGridSearchConditionSerializer))]
	public class ShotGridSearchCondition
	{
		public readonly string Field;
		public readonly string Condition;
		public readonly string Value;

		public ShotGridSearchCondition(string a_field, string a_condition, string a_value)
		{
			Field = a_field;
			Condition = a_condition;
			Value = a_value;
		}
	}

	internal class ShotGridSearchConditionSerializer : JsonConverter<ShotGridSearchCondition>
	{
		public override void WriteJson(JsonWriter writer, ShotGridSearchCondition? value, JsonSerializer serializer)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}

			writer.WriteStartArray();
			writer.WriteValue(value.Field);
			writer.WriteValue(value.Condition);
			writer.WriteValue(value.Value);
			writer.WriteEndArray();
		}

		public override ShotGridSearchCondition? ReadJson(JsonReader reader, Type objectType, ShotGridSearchCondition? existingValue,
			bool hasExistingValue, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}
	};
}
