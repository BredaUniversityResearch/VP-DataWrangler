using Newtonsoft.Json;

namespace ShotGridIntegration
{
	public class ShotGridEntityProject
	{
		public class EntityLinks
		{
			[JsonProperty("self")]
			public string? Self;
		};

		public class ProjectAttributes
		{
			[JsonProperty("name")]
			public string? Name;
		};

		[JsonProperty("attributes")]
		public ProjectAttributes Attributes = new ProjectAttributes();
		[JsonProperty("id")]
		public int Id;
		[JsonProperty("links")]
		public EntityLinks Links = new EntityLinks();
	}
}