using Newtonsoft.Json;

namespace ShotGridIntegration;

public class ShotGridEntity
{
	public static class TypeNames
	{
		public const string Project = "Project";
		public const string Shot = "Shot";
	}

	[JsonProperty("id")]
	public int Id;
	[JsonProperty("links")]
	public ShotGridEntityLinks Links = new ShotGridEntityLinks();
}