using DataApiCommon;
using Newtonsoft.Json;

namespace ShotGridIntegration
{
	public class ShotGridEntityAttachment : ShotGridEntity
	{
		public class AttachmentAttributes
		{
			[JsonProperty("name")]
			public string Name = "";
		};

		[JsonProperty("attributes")]
		public AttachmentAttributes Attributes = new AttachmentAttributes();

		public override DataEntityBase ToDataEntity()
		{
			throw new NotImplementedException();
		}
	}
}