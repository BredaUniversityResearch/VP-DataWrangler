using Newtonsoft.Json;

namespace ShotGridIntegration
{
	public class ShotGridEntityRelation : ShotGridEntity
	{
		public class RelationAttributes
		{
			[JsonProperty("id")]
			public int RelationId;
			[JsonProperty("code")]
			public string Code = "";
			[JsonProperty("type")]
			public string RelationType = "";
		};

		[JsonProperty("attributes")]
		public RelationAttributes Attributes = new RelationAttributes();
	};

	public class ShotGridEntityRelationCreateData
	{
		[JsonProperty("id")]
		public int RelationId;
		[JsonProperty("code")]
		public string Code;
		[JsonProperty("type")]
		public string RelationType;

		public ShotGridEntityRelationCreateData(int a_relationId, string a_code, string a_relationType)
		{
			RelationId = a_relationId;
			Code = a_code;
			RelationType = a_relationType;
		}

		public ShotGridEntityRelationCreateData(ShotGridEntityRelation a_relation)
		{
			RelationId = a_relation.Id;
			Code = a_relation.Attributes.Code;
			RelationType = a_relation.ShotGridType;
		}
	};
}
