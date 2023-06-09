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

		protected override DataEntityBase ToDataEntityInternal()
		{
			return new DataEntityProject() {Name = Attributes.Name};
		}
	}
}