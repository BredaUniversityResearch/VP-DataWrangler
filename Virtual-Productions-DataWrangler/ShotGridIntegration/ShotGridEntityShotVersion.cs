using AutoNotify;
using DataApiCommon;
using Newtonsoft.Json;

namespace ShotGridIntegration
{
	public partial class ShotVersionAttributes
	{
		[JsonProperty("code"), DataEntityField(nameof(DataEntityShotVersion.ShotVersionName))] 
		public string VersionCode = "";
		[JsonProperty("description"), DataEntityField(nameof(DataEntityShotVersion.Description))] 
		public string? Description;
		[JsonProperty("image"), DataEntityField(nameof(DataEntityShotVersion.ImageURL))] 
		public string? ImageURL;
		[JsonProperty("sg_datawrangler_meta"), DataEntityField(nameof(DataEntityShotVersion.DataWranglerMeta))] 
		public string? DataWranglerMeta;
		[JsonProperty("flagged"), DataEntityField(nameof(DataEntityShotVersion.Flagged))]
		public bool Flagged = false;
		[JsonProperty("sg_path_to_frames")] 
		public string? PathToFrames; /*Full-Res file path*/
	};

	public class ShotGridEntityShotVersion: ShotGridEntity
	{
		[JsonProperty("attributes")] public ShotVersionAttributes Attributes = new ShotVersionAttributes();

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

		protected override DataEntityBase ToDataEntityInternal()
		{
			return new DataEntityShotVersion()
			{
				DataWranglerMeta = Attributes.DataWranglerMeta,
				ShotVersionName = Attributes.VersionCode,
				Description = Attributes.Description,
				Flagged = Attributes.Flagged
			};
		}
	}
}