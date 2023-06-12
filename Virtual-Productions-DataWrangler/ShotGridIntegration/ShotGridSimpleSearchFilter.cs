using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace ShotGridIntegration
{
	[JsonConverter(typeof(ShotGridSimpleSearchFilterConverter))]
	internal class ShotGridSimpleSearchFilter
	{
		private readonly List<ShotGridSearchCondition> m_conditions = new List<ShotGridSearchCondition>();
		public IReadOnlyList<ShotGridSearchCondition> Conditions => m_conditions;

		public void FieldIs(string a_field, object a_status)
		{
			m_conditions.Add(new ShotGridSearchCondition(a_field, "is", a_status));
		}

		public static ShotGridSimpleSearchFilter ForProject(int a_projectId)
		{
			ShotGridSimpleSearchFilter filter = new ShotGridSimpleSearchFilter();
			filter.FieldIs("project.Project.id", a_projectId);
			return filter;
		}
	}

	internal class ShotGridSimpleSearchFilterConverter : JsonConverter<ShotGridSimpleSearchFilter>
	{
		public override void WriteJson(JsonWriter a_writer, ShotGridSimpleSearchFilter? a_value, JsonSerializer a_serializer)
		{
			if (a_value == null)
				throw new ArgumentNullException("a_value");

			a_serializer.Serialize(a_writer, a_value.Conditions);
		}

		public override ShotGridSimpleSearchFilter? ReadJson(JsonReader a_reader, Type a_objectType,
			ShotGridSimpleSearchFilter? a_existingValue, bool a_hasExistingValue, JsonSerializer a_serializer)
		{
			throw new NotImplementedException();
		}
	}
}
