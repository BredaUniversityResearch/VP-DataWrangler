using System.Reflection;
using Newtonsoft.Json;

namespace ShotGridIntegration;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ShotGridEntityTypeAttribute: Attribute
{
	public readonly string TypeName;

	public ShotGridEntityTypeAttribute(string a_typeName)
	{
		TypeName = a_typeName;
	}
};

public class ShotGridEntity
{
	public static class TypeNames
	{
		public const string Project = "Project";
		public const string Shot = "Shot";
		public const string ShotVersion = "Version";
		public const string PublishedFile = "PublishedFile";
		public const string PublishedFileType = "PublishedFileType";
		public const string LocalStorage = "LocalStorage";
		public const string Attachment = "Attachment";
	}

	[JsonProperty("id")]
	public int Id;
	[JsonProperty("type")]
	public string ShotGridType = "";
	[JsonProperty("links")]
	public ShotGridEntityLinks Links = new ShotGridEntityLinks();

	public static string GetEntityName<TEntityType>() 
		where TEntityType : ShotGridEntity
	{
		ShotGridEntityTypeAttribute? attrib = typeof(TEntityType).GetCustomAttribute<ShotGridEntityTypeAttribute>(false);
		if (attrib == null)
		{
			throw new Exception($"Shot Grid Entity {typeof(TEntityType).FullName} not decorated with ShotGridEntityTypeAttribute");
		}

		return attrib.TypeName;
	}
}