using Newtonsoft.Json;

namespace DataApiSFTP;

internal class DataApiSFTPShotAttributes
{
	[JsonProperty("entity_id")]
	public Guid EntityId = Guid.NewGuid();
}