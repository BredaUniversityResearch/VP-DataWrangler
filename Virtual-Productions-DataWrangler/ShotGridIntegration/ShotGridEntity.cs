using Newtonsoft.Json;

namespace ShotGridIntegration;

public class ShotGridEntity
{
	public static class TypeNames
	{
		public const string Project = "Project";
		public const string Shot = "Shot";
		public const string ShotVersion = "Version";
		public const string PublishedFile = "PublishedFile";
		public const string PublishedFileType = "PublishedFileType";
	}

	[JsonProperty("id")]
	public int Id;
	[JsonProperty("type")]
	public string ShotGridType = "";
	[JsonProperty("links")]
	public ShotGridEntityLinks Links = new ShotGridEntityLinks();
}