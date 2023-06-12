using DataApiCommon;
using Newtonsoft.Json;

namespace ShotGridIntegration
{
	internal class ShotGridEntityAttachment : ShotGridEntity
	{
		public class AttachmentAttributes
		{
			[JsonProperty("name")]
			public string Name = "";
		};

		[JsonProperty("attributes")]
		public AttachmentAttributes Attributes = new AttachmentAttributes();

		protected override DataEntityBase ToDataEntityInternal()
		{
			throw new NotImplementedException();
		}
	}
}