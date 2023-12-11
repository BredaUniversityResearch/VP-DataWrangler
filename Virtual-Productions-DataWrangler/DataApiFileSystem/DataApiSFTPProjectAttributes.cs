using DataApiCommon;
using Newtonsoft.Json;

namespace DataApiSFTP
{
	internal class DataApiSFTPProjectAttributes
	{
		[JsonProperty("active")]
		public bool Active = true;

		[JsonIgnore]
		public string ProjectName = "";

		[JsonProperty("entity_id")]
		public Guid EntityId = Guid.NewGuid();

		[JsonProperty("data_store_id")]
		public Guid DataStoreId = Guid.Empty;

		public DataApiSFTPProjectAttributes()
		{
		}

		public DataApiSFTPProjectAttributes(DataEntityProject a_project)
		{
			Active = true;
			ProjectName = a_project.Name;
			EntityId = a_project.EntityId;
			DataStoreId = a_project.DataStore?.EntityId ?? Guid.Empty;
		}

		public DataEntityProject ToDataEntity()
		{
			return new DataEntityProject
			{
				EntityId = EntityId,
				Name = ProjectName,
				DataStore = ((DataStoreId != Guid.Empty) ? new DataEntityReference(typeof(DataEntityLocalStorage), DataStoreId) : null)
			};
		}
	}
}
