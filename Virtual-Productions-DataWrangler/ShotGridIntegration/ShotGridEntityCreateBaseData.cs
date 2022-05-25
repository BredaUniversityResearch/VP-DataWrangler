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
		[JsonProperty("project")]
		public ShotGridEntityReference Project;
		[JsonProperty("entity")]
		public ShotGridEntityReference ParentShotGridEntity;

		public ShotGridEntityCreateBaseData(int a_projectId, string a_parentEntityType, int a_parentEntityId)
		{
			Project = new ShotGridEntityReference(ShotGridEntity.TypeNames.Project, a_projectId);
			ParentShotGridEntity = new ShotGridEntityReference(a_parentEntityType, a_parentEntityId);
		}
	}
}
