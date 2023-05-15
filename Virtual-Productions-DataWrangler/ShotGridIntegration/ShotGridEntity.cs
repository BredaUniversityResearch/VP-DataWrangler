using System.Reflection;
using Newtonsoft.Json;

namespace ShotGridIntegration;

public class ShotGridEntity
{
	[JsonProperty("id")]
	public int Id;
	[JsonProperty("type")]
	public ShotGridEntityName ShotGridType;
	[JsonProperty("links")]
	public ShotGridEntityLinks Links = new ShotGridEntityLinks();
	[JsonProperty("relationships")]
	public ShotGridEntityRelationships EntityRelationships = new ShotGridEntityRelationships();

	public ShotGridEntityChangeTracker ChangeTracker;

	protected ShotGridEntity()
	{
		ChangeTracker = new ShotGridEntityChangeTracker(this);
		ShotGridType = ShotGridEntityName.FromType(GetType());
	}
}

public class ShotGridEntityRelationships
{
	[JsonProperty("project"), JsonConverter(typeof(JsonConverterShotGridEntityReferenceRelationships))]
	public ShotGridEntityReference? Project;
}