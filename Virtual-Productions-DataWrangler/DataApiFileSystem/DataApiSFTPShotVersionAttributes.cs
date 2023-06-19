using DataApiCommon;
using Newtonsoft.Json;

namespace DataApiSFTP;

public class DataApiSFTPShotVersionAttributes
{
	[JsonProperty("entity_id")]
	public Guid EntityId = Guid.NewGuid();

	[JsonIgnore]
	public string ShotVersionName = "";

	[JsonProperty("datawrangler_meta")]
	public string DataWranglerMeta = "";

	[JsonProperty("description")]
	public string Description = "";

	[JsonProperty("image_url")]
	public string? ImageURL;

	[JsonProperty("flagged")]
	public bool Flagged;

	public DataApiSFTPShotVersionAttributes()
	{
	}

	public DataApiSFTPShotVersionAttributes(DataEntityShotVersion a_version)
	{
		EntityId = a_version.EntityId;
		ShotVersionName = a_version.ShotVersionName;
		DataWranglerMeta = a_version.DataWranglerMeta?? "";
		Description = a_version.Description ?? "";
		ImageURL = a_version.ImageURL;
		Flagged = a_version.Flagged;
	}

	public DataEntityShotVersion ToDataEntity(DataEntityProject a_project, DataEntityShot a_shot)
	{
		return new DataEntityShotVersion
		{
			ShotVersionName = ShotVersionName,
			DataWranglerMeta = DataWranglerMeta,
			Description = Description,
			EntityId = EntityId,
			EntityRelationships = {
				Parent = new DataEntityReference(a_shot),
				Project = new DataEntityReference(a_project)
			},
			Flagged = Flagged,
			ImageURL = ImageURL
		};
	}
}