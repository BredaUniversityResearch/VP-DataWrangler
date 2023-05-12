using System.Reflection;
using Newtonsoft.Json;

namespace ShotGridIntegration;

public class ShotGridEntity
{
	[JsonProperty("id")]
	public int Id;
	[JsonProperty("type")]
	public ShotGridEntityName ShotGridType = ShotGridEntityName.Invalid;
	[JsonProperty("links")]
	public ShotGridEntityLinks Links = new ShotGridEntityLinks();
	[JsonProperty("relationships")]
	public ShotGridEntityRelationships EntityRelationships = new ShotGridEntityRelationships();

	public ShotGridEntityChangeTracker ChangeTracker;

	protected ShotGridEntity()
	{
		ChangeTracker = new ShotGridEntityChangeTracker(this);
	}
}

public class ShotGridEntityRelationships
{
	[JsonProperty("project"), JsonConverter(typeof(JsonConverterShotGridEntityReferenceRelationships))]
	public ShotGridEntityReference? Project;
}