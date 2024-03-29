using System.Runtime.Serialization;
using DataApiCommon;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ShotGridIntegration
{
	public enum EActivityUpdateType
	{
		[EnumMember(Value = "create")]
		Create,
		[EnumMember(Value = "update")]
		Update,
		[EnumMember(Value = "create_reply")]
		CreateReply,
		[EnumMember(Value = "delete")]
		Delete
	}

	internal class ShotGridEntityActivityStreamResponse : ShotGridEntity
	{
		public class ShotGridActivityUpdate
		{
			[JsonProperty("id")]
			public int Id = 0;

			[JsonProperty("update_type")]
			public EActivityUpdateType UpdateType = EActivityUpdateType.Create;

			[JsonProperty("meta")]
			public JObject? Meta = null;

			[JsonProperty("primary_entity")]
			public ShotGridEntityReference? PrimaryEntity = null;
		}

		[JsonProperty("entity_type")]
		public string EntityType = string.Empty;
		[JsonProperty("entity_id")]
		public int EntityId = 0;
		[JsonProperty("latest_update_id")]
		public int LatestUpdateId = 0;
		[JsonProperty("earliest_update_id")]
		public int EarliestUpdateId = 0;
		[JsonProperty("updates")]
		public ShotGridActivityUpdate[] Updates = Array.Empty<ShotGridActivityUpdate>();

		protected override DataEntityBase ToDataEntityInternal()
		{
			throw new NotImplementedException();
		}
	}
}