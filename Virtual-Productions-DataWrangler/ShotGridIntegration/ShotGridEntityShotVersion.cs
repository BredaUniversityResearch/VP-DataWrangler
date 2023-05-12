using AutoNotify;
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
	}
}