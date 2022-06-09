using Newtonsoft.Json;

namespace ShotGridIntegration
{
	[ShotGridEntityType(TypeNames.Attachment)]
	public class ShotGridEntityAttachment : ShotGridEntity
	{
		public class AttachmentAttributes
		{
			[JsonProperty("name")]
			public string Name = "";
		};

		[JsonProperty("attributes")]
		public AttachmentAttributes Attributes = new AttachmentAttributes();
	}
}