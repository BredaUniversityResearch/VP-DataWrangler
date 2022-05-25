using Newtonsoft.Json;

namespace ShotGridIntegration;

public class ShotGridEntityReference
{
	[JsonProperty("type")] public string EntityType;
	[JsonProperty("id")] public int Id;

	public ShotGridEntityReference(ShotGridEntity a_target)
	{
		EntityType = a_target.ShotGridType;
		Id = a_target.Id;
	}

	public ShotGridEntityReference(string a_entityType, int a_entityId)
	{
		EntityType = a_entityType;
		Id = a_entityId;
	}
};