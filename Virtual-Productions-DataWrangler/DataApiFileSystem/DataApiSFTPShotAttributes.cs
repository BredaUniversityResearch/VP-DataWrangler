using DataApiCommon;
using Newtonsoft.Json;

namespace DataApiSFTP;

internal class DataApiSFTPShotAttributes
{
	[JsonProperty("entity_id")]
	public Guid EntityId = Guid.NewGuid();

	[JsonProperty("shot_name")]
	public string ShotName = "";

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
	}

	public DataEntityShot ToDataEntity(DataEntityProject a_ownerProject)
	{
		return new DataEntityShot
		{
			EntityId = EntityId,
			ShotName = ShotName,
			EntityRelationships =
			{
				Project = new DataEntityReference(a_ownerProject)
			}
		};
	}
}