using Newtonsoft.Json;

namespace ShotGridIntegration
{
	internal class ShotGridEntityCreateBaseData
	{
		[JsonProperty("project")]
		public ShotGridEntityReference Project;
		[JsonProperty("entity")]
		public ShotGridEntityReference? ParentShotGridEntity = null;

		public ShotGridEntityCreateBaseData(int a_projectId, ShotGridEntityReference? a_parentEntity)
		{
			Project = new ShotGridEntityReference(ShotGridEntityTypeInfo.Project, a_projectId);
			ParentShotGridEntity = a_parentEntity;
		}
	}
}
