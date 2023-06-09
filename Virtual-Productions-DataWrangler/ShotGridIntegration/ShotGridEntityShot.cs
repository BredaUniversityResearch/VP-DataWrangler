using AutoNotify;
using DataApiCommon;
using Newtonsoft.Json;

namespace ShotGridIntegration
{
	public partial class ShotGridEntityShotAttributes
	{
		[JsonProperty("code"), AutoNotify] private string m_shotCode = "";
		[JsonProperty("description"), AutoNotify] private string m_description = "";
		[JsonProperty("image"), AutoNotify] private string? m_imageURL;
		[JsonProperty("sg_camera"), AutoNotify] private string? m_camera;
		[JsonProperty("sg_lens"), AutoNotify] private string? m_lens;
		[JsonProperty("project"), AutoNotify] private ShotGridEntityReference? m_project;
	};

	public class ShotGridEntityShot: ShotGridEntity
	{
		[JsonProperty("attributes")]
		public readonly ShotGridEntityShotAttributes Attributes = new ShotGridEntityShotAttributes();

		public ShotGridEntityShot()
		{
		}

		public ShotGridEntityShot(DataEntityShot a_shot)
			: base(a_shot)
		{
			Attributes.ShotCode = a_shot.ShotName;

		}

		protected override DataEntityShot ToDataEntityInternal()
		{
			return new DataEntityShot() {ShotName = Attributes.ShotCode};
		}
	}
}