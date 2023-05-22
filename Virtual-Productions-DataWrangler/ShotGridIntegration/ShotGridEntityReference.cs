using Newtonsoft.Json;

namespace ShotGridIntegration;

public class ShotGridEntityReference
{
	[JsonProperty("type")]
	public string? EntityType = null;

	[JsonProperty("id")]
	public int Id = 0;

	[JsonProperty("name")]
	public string? EntityName = null;

	public ShotGridEntityReference()
	{
	}

	public ShotGridEntityReference(ShotGridEntityName a_entityType, int a_entityId)
	{
		EntityType = a_entityType.CamelCase;
		Id = a_entityId;
	}

	public static ShotGridEntityReference Create<TEntityType>(TEntityType a_target)
		where TEntityType : ShotGridEntity
	{
		return Create(ShotGridEntityName.FromType<TEntityType>(), a_target);
	}

	public static ShotGridEntityReference Create(ShotGridEntityName a_shotGridTypeName, ShotGridEntity a_target)
	{
		ShotGridEntityReference entityRef = new ShotGridEntityReference(a_shotGridTypeName, a_target.Id);
		return entityRef;
	}
};