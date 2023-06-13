using DataApiCommon;
using Newtonsoft.Json;

namespace DataApiSFTP
{
	internal class DataApiSFTPProjectAttributes
	{
		[JsonProperty("active")]
		public bool Active = true;

		[JsonProperty("project_name")]
		public string ProjectName = "";

		[JsonProperty("entity_id")]
		public Guid EntityId = Guid.NewGuid();

		public DataApiSFTPProjectAttributes()
		{
		}

		public DataApiSFTPProjectAttributes(DataEntityProject a_project)
		{
			Active = true;
			ProjectName = a_project.Name;
			EntityId = a_project.EntityId;
		}

		public DataEntityProject ToDataEntity()
		{
			return new DataEntityProject
			{
				EntityId = EntityId,
				Name = ProjectName
			};
		}
	}
}
