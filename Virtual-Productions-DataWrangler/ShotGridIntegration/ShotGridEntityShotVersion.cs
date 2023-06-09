using AutoNotify;
using DataApiCommon;
using Newtonsoft.Json;

namespace ShotGridIntegration
{
	public partial class ShotVersionAttributes
	{
		[AutoNotify, JsonProperty("code")] private string m_versionCode = "";
		[AutoNotify, JsonProperty("description")] private string? m_description;
		[AutoNotify, JsonProperty("image")] private string? m_imageURL;
		[AutoNotify, JsonProperty("sg_datawrangler_meta")] private string? m_dataWranglerMeta;
		[AutoNotify, JsonProperty("flagged")] private bool m_flagged = false;
		[AutoNotify, JsonProperty("sg_path_to_frames")] private string? m_pathToFrames; /*Full-Res file path*/
	};

	public class ShotGridEntityShotVersion: ShotGridEntity
	{
		[JsonProperty("attributes")] public ShotVersionAttributes Attributes { get; set; } = new ShotVersionAttributes();

		public ShotGridEntityShotVersion()
		{
		}

		public ShotGridEntityShotVersion(DataEntityShotVersion a_version)
			: base(a_version)
		{
			Attributes.Description = a_version.Description;
			Attributes.VersionCode = a_version.ShotVersionName;
			Attributes.ImageURL = a_version.ImageURL;
			Attributes.DataWranglerMeta = a_version.DataWranglerMeta;
			Attributes.Flagged = a_version.Flagged;
		}

		public override DataEntityBase ToDataEntity()
		{
			DataEntityShotVersion result = new DataEntityShotVersion()
			{
				DataWranglerMeta = Attributes.DataWranglerMeta,
				ShotVersionName = Attributes.VersionCode,
				Description = Attributes.Description,
				Flagged = Attributes.Flagged
			};
			CopyToDataEntity(result);
			return result;
		}
	}
}