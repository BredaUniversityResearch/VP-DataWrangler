using Newtonsoft.Json;

namespace ShotGridIntegration
{
	public class ShotGridEntityShot: ShotGridEntity
	{
		public class ShotAttributes
		{
			[JsonProperty("code")] public string ShotCode = "";
			[JsonProperty("description")] public string Description = "";
			[JsonProperty("image")] public string ImageURL = "";
		};

		[JsonProperty("attributes")]
		public ShotAttributes Attributes = new ShotAttributes();
	}
}