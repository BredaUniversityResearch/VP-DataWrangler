using System.Reflection;
using System.Text;

namespace ShotGridIntegration;

public class ShotGridEntityName
{
	public static readonly ShotGridEntityName Invalid = new ShotGridEntityName(typeof(ShotGridEntity), "Invalid");

	public static readonly ShotGridEntityName Project = new ShotGridEntityName(typeof(ShotGridEntityProject), "Project");
	public static readonly ShotGridEntityName Shot = new ShotGridEntityName(typeof(ShotGridEntityShot), "Shot");
	public static readonly ShotGridEntityName ShotVersion = new ShotGridEntityName(typeof(ShotGridEntityShotVersion), "Version");
	public static readonly ShotGridEntityName PublishedFile = new ShotGridEntityName(typeof(ShotGridEntityFilePublish), "PublishedFile");
	public static readonly ShotGridEntityName PublishedFileType = new ShotGridEntityName(typeof(ShotGridEntityRelation), "PublishedFileType");
	public static readonly ShotGridEntityName LocalStorage = new ShotGridEntityName(typeof(ShotGridEntityLocalStorage), "LocalStorage");
	public static readonly ShotGridEntityName Attachment = new ShotGridEntityName(typeof(ShotGridEntityAttachment), "Attachment");
	public static readonly ShotGridEntityName ActivityStream = new ShotGridEntityName(typeof(ShotGridEntityActivityStreamResponse), "ActivityStream");

	public readonly Type ImplementedType;
	public readonly string CamelCase;
	public readonly string SnakeCasePlural;

	private static readonly ShotGridEntityName[] AllNames;

	static ShotGridEntityName()
	{
		FieldInfo[] allStaticFields =typeof(ShotGridEntityName).GetFields(BindingFlags.Static | BindingFlags.Public);
		List<ShotGridEntityName> allNamesList = new List<ShotGridEntityName>(allStaticFields.Length);
		foreach (FieldInfo field in allStaticFields)
		{
			if (field.FieldType == typeof(ShotGridEntityName))
			{
				ShotGridEntityName? definedName = (ShotGridEntityName?)field.GetValue(null);
				if (definedName == null)
				{
					throw new Exception("Failed to get value from field");
				}

				allNamesList.Add(definedName);
			}
		}

		AllNames = allNamesList.ToArray();
	}

	private ShotGridEntityName(Type a_implementedType, string a_entityCamelCase)
	{
		ImplementedType = a_implementedType;
		CamelCase = a_entityCamelCase;
		SnakeCasePlural = ToSnakeCasePlural(a_entityCamelCase);
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

	public static ShotGridEntityName FromType(Type a_entityType)
	{
		if (a_entityType.IsArray)
		{
			return FromType(a_entityType.GetElementType()!);
		}

		if (a_entityType.IsAssignableFrom(typeof(ShotGridEntity)))
		{
			throw new Exception($"Shot Grid Entity {a_entityType.FullName} not derived from ShotGridEntityy");
		}

		ShotGridEntityName? name = Array.Find(AllNames, a_obj => a_obj.ImplementedType == a_entityType);
		if (name == null)
		{
			throw new Exception($"Shot Grid Entity {a_entityType.FullName} not defined in all entity type names");
		}

		return name;
	}

	public static ShotGridEntityName FromType<TEntityType>()
		where TEntityType: ShotGridEntity
	{
		return FromType(typeof(TEntityType));
	}

	public static ShotGridEntityName FromCamelCaseName(string a_typeName)
	{
		ShotGridEntityName? name = Array.Find(AllNames, a_obj => a_obj.CamelCase == a_typeName);
		if (name == null)
		{
			throw new Exception($"Shot Grid Entity {a_typeName} not defined in all entity type names");
		}

		return name;
	}

	public class NameTypeEqualityComparer : IEqualityComparer<ShotGridEntityName>
	{
		public bool Equals(ShotGridEntityName? x, ShotGridEntityName? y)
		{
			if (x != null && y != null)
			{
				return x.ImplementedType == y.ImplementedType;
			}

			return false;
		}

		public int GetHashCode(ShotGridEntityName obj)
		{
			return obj.ImplementedType.GetHashCode();
		}
	}
};