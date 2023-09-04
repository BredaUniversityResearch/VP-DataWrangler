using DataApiCommon;
using Newtonsoft.Json;

namespace DataApiSFTP;

public class DataApiSFTPShotAttributes
{
	[JsonProperty("entity_id")]
	public Guid EntityId = Guid.NewGuid();

	[JsonIgnore]
	public string ShotName = "";

	[JsonProperty("description")]
	public string Description = "";

	[JsonProperty("data_sources_template", ItemConverterType = typeof(IngestDataSourceMetaConverter))]
	public List<IngestDataSourceMeta> DataSourcesTemplate = new List<IngestDataSourceMeta>();

	public DataApiSFTPShotAttributes()
	{
	}

	public DataApiSFTPShotAttributes(string a_shotName)
	{
		ShotName = a_shotName;
	}

	public DataApiSFTPShotAttributes(DataEntityShot a_dataEntity)
	{
		EntityId = a_dataEntity.EntityId;
		ShotName = a_dataEntity.ShotName;
		Description = a_dataEntity.Description;
		DataSourcesTemplate = a_dataEntity.DataSourcesTemplate.FileSources;
	}

	public DataEntityShot ToDataEntity(DataEntityProject a_ownerProject)
	{
		return new DataEntityShot
		{
			EntityId = EntityId,
			ShotName = ShotName,
			Description = Description,
			DataSourcesTemplate = new IngestDataShotVersionMeta(DataSourcesTemplate),
			EntityRelationships =
			{
				Project = new DataEntityReference(a_ownerProject)
			}
		};
	}
}