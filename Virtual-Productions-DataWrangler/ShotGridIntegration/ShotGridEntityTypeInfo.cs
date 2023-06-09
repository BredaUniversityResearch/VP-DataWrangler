using System.Reflection;
using System.Text;
using DataApiCommon;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ShotGridIntegration;

public class ShotGridEntityTypeInfo
{
	public static readonly ShotGridEntityTypeInfo Invalid = new ShotGridEntityTypeInfo(typeof(ShotGridEntity), "Invalid", null);

	public static readonly ShotGridEntityTypeInfo Project = new ShotGridEntityTypeInfo(typeof(ShotGridEntityProject), "Project", typeof(DataEntityProject));
	public static readonly ShotGridEntityTypeInfo Shot = new ShotGridEntityTypeInfo(typeof(ShotGridEntityShot), "Shot", typeof(DataEntityShot));
	public static readonly ShotGridEntityTypeInfo ShotVersion = new ShotGridEntityTypeInfo(typeof(ShotGridEntityShotVersion), "Version", typeof(DataEntityShotVersion));
	public static readonly ShotGridEntityTypeInfo PublishedFile = new ShotGridEntityTypeInfo(typeof(ShotGridEntityFilePublish), "PublishedFile", typeof(DataEntityFilePublish));
	public static readonly ShotGridEntityTypeInfo PublishedFileType = new ShotGridEntityTypeInfo(typeof(ShotGridEntityRelation), "PublishedFileType", typeof(DataEntityPublishedFileType));
	public static readonly ShotGridEntityTypeInfo LocalStorage = new ShotGridEntityTypeInfo(typeof(ShotGridEntityLocalStorage), "LocalStorage", typeof(DataEntityLocalStorage));
	public static readonly ShotGridEntityTypeInfo Attachment = new ShotGridEntityTypeInfo(typeof(ShotGridEntityAttachment), "Attachment", null);
	public static readonly ShotGridEntityTypeInfo ActivityStream = new ShotGridEntityTypeInfo(typeof(ShotGridEntityActivityStreamResponse), "ActivityStream", null);

	public readonly Type ImplementedType;
	public readonly string CamelCase;
	public readonly string SnakeCasePlural;
	public readonly Type? DataEntityType;

	private static readonly ShotGridEntityTypeInfo[] AllEntityTypes;
	private readonly Dictionary<string, string> m_dataEntityFieldToShotGridPath = new Dictionary<string, string>();

	static ShotGridEntityTypeInfo()
	{
		FieldInfo[] allStaticFields = typeof(ShotGridEntityTypeInfo).GetFields(BindingFlags.Static | BindingFlags.Public);
		List<ShotGridEntityTypeInfo> allNamesList = new List<ShotGridEntityTypeInfo>(allStaticFields.Length);
		foreach (FieldInfo field in allStaticFields)
		{
			if (field.FieldType == typeof(ShotGridEntityTypeInfo))
			{
				ShotGridEntityTypeInfo? definedName = (ShotGridEntityTypeInfo?)field.GetValue(null);
				if (definedName == null)
				{
					throw new Exception("Failed to get value from field");
				}

				allNamesList.Add(definedName);
			}
		}

		AllEntityTypes = allNamesList.ToArray();
	}

	private ShotGridEntityTypeInfo(Type a_implementedType, string a_entityCamelCase, Type? a_dataEntityType)
	{
		ImplementedType = a_implementedType;
		CamelCase = a_entityCamelCase;
		DataEntityType = a_dataEntityType;
		SnakeCasePlural = ToSnakeCasePlural(a_entityCamelCase);
		BuildFieldConversionList(a_implementedType);
	}

	private void BuildFieldConversionList(Type a_type)
	{
		FieldInfo[] availableFields = a_type.GetFields(BindingFlags.Instance | BindingFlags.Public);
		foreach (FieldInfo field in availableFields)
		{
			JsonPropertyAttribute? jsonName = field.GetCustomAttribute<JsonPropertyAttribute>();
			if (jsonName != null)
			{
				DataEntityFieldAttribute? fieldAttribute = field.GetCustomAttribute<DataEntityFieldAttribute>();
				string thisFieldPath = jsonName.PropertyName!;
				if (fieldAttribute != null)
				{
					m_dataEntityFieldToShotGridPath.Add(fieldAttribute.DataEntityFieldName, thisFieldPath);
				}
				else
				{
					BuildFieldConversionList(field.FieldType);
				}
			}
		}
	}

	private string ToSnakeCasePlural(string a_string)
	{
		StringBuilder sb = new StringBuilder(32);
		foreach (char c in a_string)
		{
			if (char.IsUpper(c))
			{
				if (sb.Length > 0)
				{
					sb.Append('_');
				}
				sb.Append(char.ToLowerInvariant(c));
			}
			else
			{
				sb.Append(c);
			}
		}

		sb.Append('s');

		return sb.ToString();
	}

	public static ShotGridEntityTypeInfo FromType(Type a_entityType)
	{
		if (a_entityType.IsArray)
		{
			return FromType(a_entityType.GetElementType()!);
		}

		if (a_entityType.IsAssignableFrom(typeof(ShotGridEntity)))
		{
			throw new Exception($"Shot Grid Entity {a_entityType.FullName} not derived from ShotGridEntityy");
		}

		ShotGridEntityTypeInfo? name = Array.Find(AllEntityTypes, a_obj => a_obj.ImplementedType == a_entityType);
		if (name == null)
		{
			throw new Exception($"Shot Grid Entity {a_entityType.FullName} not defined in all entity type names");
		}

		return name;
	}

	public static ShotGridEntityTypeInfo FromType<TEntityType>()
		where TEntityType: ShotGridEntity
	{
		return FromType(typeof(TEntityType));
	}

	public static ShotGridEntityTypeInfo FromCamelCaseName(string a_typeName)
	{
		ShotGridEntityTypeInfo? name = Array.Find(AllEntityTypes, a_obj => a_obj.CamelCase == a_typeName);
		if (name == null)
		{
			throw new Exception($"Shot Grid Entity {a_typeName} not defined in all entity type names");
		}

		return name;
	}

	public class NameTypeEqualityComparer : IEqualityComparer<ShotGridEntityTypeInfo>
	{
		public bool Equals(ShotGridEntityTypeInfo? x, ShotGridEntityTypeInfo? y)
		{
			if (x != null && y != null)
			{
				return x.ImplementedType == y.ImplementedType;
			}

			return false;
		}

		public int GetHashCode(ShotGridEntityTypeInfo obj)
		{
			return obj.ImplementedType.GetHashCode();
		}
	}

	public static ShotGridEntityTypeInfo FromDataEntityType(Type a_entityType)
	{
		foreach (ShotGridEntityTypeInfo ent in AllEntityTypes)
		{
			if (ent.DataEntityType == a_entityType)
			{
				return ent;
			}
		}

		throw new Exception("");
	}

	public string GetShotGridFieldPathFromDataEntityFieldName(string a_keyName)
	{
		if (m_dataEntityFieldToShotGridPath.TryGetValue(a_keyName, out string? shotGridName))
		{
			return shotGridName;
		}

		throw new Exception($"Failed to find ShotGrid field path for EntityField {a_keyName}");
	}
};