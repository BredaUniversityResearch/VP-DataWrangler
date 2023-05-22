using System.Reflection;
using Newtonsoft.Json;

namespace ShotGridIntegration;

public static class JsonAttributeFieldEnumerator
{
	public static string[] Get<TTypeToEnumerate>()
	{
		List<string> fieldNames = new List<string>();
		EnumerateTypeFields(typeof(TTypeToEnumerate), fieldNames);
		if (fieldNames.Count == 0)
		{
			throw new Exception("Expecting at least one field ");
		}

		EnumerateTypeFields(typeof(ShotGridEntityRelationships), fieldNames);

		return fieldNames.ToArray();
	}

	private static void EnumerateTypeFields(Type a_targetType, List<string> a_resultFieldNames)
	{
		FieldInfo[] fields = a_targetType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
		foreach (FieldInfo info in fields)
		{
			JsonPropertyAttribute? prop = info.GetCustomAttribute<JsonPropertyAttribute>(false);
			if (prop != null)
			{
				a_resultFieldNames.Add(prop.PropertyName!);
			}
		}
	}
}