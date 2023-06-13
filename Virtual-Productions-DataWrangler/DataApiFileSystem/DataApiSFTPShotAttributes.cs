using DataApiCommon;
using Newtonsoft.Json;

namespace DataApiSFTP;

internal class DataApiSFTPShotAttributes
{
	[JsonProperty("entity_id")]
	public Guid EntityId = Guid.NewGuid();

	[JsonProperty("shot_name")]
	public string ShotName = "";

	[JsonProperty("description")]
	public string Description = "";

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
	}

	public DataEntityShot ToDataEntity(DataEntityProject a_ownerProject)
	{
		return new DataEntityShot
		{
			EntityId = EntityId,
			ShotName = ShotName,
			Description = Description,
			EntityRelationships =
			{
				Project = new DataEntityReference(a_ownerProject)
			}
		};
	}
}