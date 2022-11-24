using Newtonsoft.Json;
using System.Text;

namespace ShotGridIntegration;

[JsonConverter(typeof(ShotGridSortSpecifierConverter))]
public class ShotGridSortSpecifier
{
	private StringBuilder SortQuery = new StringBuilder(32);
	public string FullQuery => SortQuery.ToString();

	public ShotGridSortSpecifier()
	{
	}

	public ShotGridSortSpecifier(string a_fieldName, bool a_ascendingOrder)
	{
		SortOnField(a_fieldName, a_ascendingOrder);
	}

	public void SortOnField(string a_fieldName, bool a_ascendingOrder = true)
	{
		if (SortQuery.Length != 0)
		{
			SortQuery.Append(',');
		}

		if (!a_ascendingOrder)
		{
			SortQuery.Append('-');
		}

		SortQuery.Append(a_fieldName);
	}
}

internal class ShotGridSortSpecifierConverter : JsonConverter<ShotGridSortSpecifier>
{
	public override void WriteJson(JsonWriter writer, ShotGridSortSpecifier? value, JsonSerializer serializer)
	{
		if (value == null)
			throw new ArgumentNullException("value");

		serializer.Serialize(writer, value.FullQuery);
	}

	public override ShotGridSortSpecifier? ReadJson(JsonReader reader, Type objectType,
		ShotGridSortSpecifier? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		throw new NotImplementedException();
	}
}