using Newtonsoft.Json;

namespace ShotGridIntegration;

public class ShotGridEntity
{
	[JsonProperty("id")]
	public int Id;
	[JsonProperty("links")]
	public ShotGridEntityLinks Links = new ShotGridEntityLinks();
}