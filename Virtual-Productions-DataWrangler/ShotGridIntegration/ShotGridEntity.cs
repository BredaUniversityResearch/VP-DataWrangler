using System.Reflection;
using Newtonsoft.Json;

namespace ShotGridIntegration;

public class ShotGridEntity
{
	[JsonProperty("id")]
	public int Id;
	[JsonProperty("type")]
	public ShotGridEntityName? ShotGridType;
	[JsonProperty("links")]
	public ShotGridEntityLinks Links = new ShotGridEntityLinks();
}