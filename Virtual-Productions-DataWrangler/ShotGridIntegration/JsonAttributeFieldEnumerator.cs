using System.Reflection;
using Newtonsoft.Json;

namespace ShotGridIntegration;

public class JsonAttributeFieldEnumerator<TTypeToEnumerate>
{
	public string[] Get()
	{
		List<string> fieldNames = new List<string>();

		FieldInfo[] fields = typeof(TTypeToEnumerate).GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		foreach (FieldInfo info in fields)
		{
			JsonPropertyAttribute? prop = info.GetCustomAttribute<JsonPropertyAttribute>(false);
			if (prop != null)
			{
				fieldNames.Add(prop.PropertyName!);
			}
		}

		if (fieldNames.Count == 0)
		{
			throw new Exception("Expecting at least one field ");
		}

		return fieldNames.ToArray();
	}
}