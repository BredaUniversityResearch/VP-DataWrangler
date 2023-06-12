using Newtonsoft.Json;

namespace DataApiSFTP
{
	internal class DataApiSFTPProjectAttributes
	{
		[JsonProperty("active")]
		public bool Active = true;

		[JsonProperty("entity_id")]
		public Guid EntityId = Guid.NewGuid();
	}
}
