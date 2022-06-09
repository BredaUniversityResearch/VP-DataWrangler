using Newtonsoft.Json;

namespace ShotGridIntegration;

public class ShotGridEntityReference
{
	[JsonProperty("type")] public string EntityType;
	[JsonProperty("id")] public int Id;

	public ShotGridEntityReference(string a_entityType, int a_entityId)
	{
		EntityType = a_entityType;
		Id = a_entityId;
	}

	public static ShotGridEntityReference Create<TEntityType>(TEntityType a_target)
		where TEntityType : ShotGridEntity
	{
		return Create(ShotGridEntity.GetEntityName<TEntityType>(), a_target);
	}

	public static ShotGridEntityReference Create(string a_shotGridTypeName, ShotGridEntity a_target)
	{
		ShotGridEntityReference entityRef = new ShotGridEntityReference(a_shotGridTypeName, a_target.Id);
		return entityRef;
	}
};