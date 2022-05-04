using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ShotGridIntegration
{
	public class ShotGridEntityCreateBaseData
	{
		public class EntityReference
		{
			[JsonProperty("type")] public string EntityType;
			[JsonProperty("id")] public int Id;

			public EntityReference(string a_entityType, int a_entityId)
			{
				EntityType = a_entityType;
				Id = a_entityId;
			}
		};

		[JsonProperty("project")]
		public EntityReference Project;
		[JsonProperty("entity")]
		public EntityReference ParentEntity;

		public ShotGridEntityCreateBaseData(int a_projectId, string a_parentEntityType, int a_parentEntityId)
		{
			Project = new EntityReference(ShotGridEntity.TypeNames.Project, a_projectId);
			ParentEntity = new EntityReference(a_parentEntityType, a_parentEntityId);
		}
	}
}
