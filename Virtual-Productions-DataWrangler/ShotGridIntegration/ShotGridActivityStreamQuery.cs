using Newtonsoft.Json;

namespace ShotGridIntegration;

public class ShotGridActivityStreamQuery
{
	[JsonProperty("min_id")]
	public int? MinId;
	[JsonProperty("max_id")]
	public int? MaxId;
	[JsonProperty("limit")]
	public int? RecordLimit;

	//entity_fields
}