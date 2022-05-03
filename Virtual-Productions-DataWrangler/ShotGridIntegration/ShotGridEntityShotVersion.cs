using Newtonsoft.Json;

namespace ShotGridIntegration
{
	public class ShotGridEntityShotVersion: ShotGridEntity
	{
		public class ShotVersionAttributes
		{
			[JsonProperty("code")] public string VersionCode = "";
			[JsonProperty("description")] public string Description = "";
			[JsonProperty("image")] public string ImageURL = "";
		};

		[JsonProperty("attributes")]
		public ShotVersionAttributes Attributes = new ShotVersionAttributes();
	}
}