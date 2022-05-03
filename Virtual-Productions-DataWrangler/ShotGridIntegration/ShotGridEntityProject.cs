using Newtonsoft.Json;

namespace ShotGridIntegration
{
	public class ShotGridEntityProject: ShotGridEntity
	{
		public class ProjectAttributes
		{
			[JsonProperty("name")]
			public string? Name;
		};

		[JsonProperty("attributes")]
		public ProjectAttributes Attributes = new ProjectAttributes();
	}
}