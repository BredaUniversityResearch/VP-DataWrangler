using DataApiCommon;
using Newtonsoft.Json;

namespace ShotGridIntegration
{
	public class ShotGridEntityProject : ShotGridEntity
	{
		public class ProjectAttributes
		{
			[JsonProperty("name")]
			public string Name = "";
		};

		[JsonProperty("attributes")]
		public ProjectAttributes Attributes = new ProjectAttributes();

		public override DataEntityProject ToDataEntity()
		{
			DataEntityProject project = new DataEntityProject() {Name = Attributes.Name};
			CopyToDataEntity(project);
			return project;
		}
	}
}