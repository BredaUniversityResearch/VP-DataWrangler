using DataApiCommon;
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

		protected override DataEntityBase ToDataEntityInternal()
		{
			throw new NotImplementedException();
		}
	};

	public class ShotGridEntityRelationCreateData
	{
		[JsonProperty("id")]
		public int RelationId;
		[JsonProperty("code")]
		public string Code;
		[JsonProperty("type")]
		public ShotGridEntityTypeInfo RelationType;

		public ShotGridEntityRelationCreateData(int a_relationId, string a_code, ShotGridEntityTypeInfo a_relationType)
		{
			RelationId = a_relationId;
			Code = a_code;
			RelationType = a_relationType;
		}

		public ShotGridEntityRelationCreateData(ShotGridEntityRelation a_relation)
		{
			RelationId = a_relation.Id;
			Code = a_relation.Attributes.Code;
			if (a_relation.ShotGridType == null)
			{
				throw new Exception("Could not setup relation creation data from relation with null type");
			}

			RelationType = a_relation.ShotGridType!;
		}
	};
}
