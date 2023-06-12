using DataApiCommon;
using Newtonsoft.Json;

namespace ShotGridIntegration;

internal class ShotGridEntityReference
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

	public ShotGridEntityReference(DataEntityReference a_dataEntityReference)
	{
		EntityName = a_dataEntityReference.EntityName;
		Id = a_dataEntityReference.EntityId;
		EntityType = ShotGridEntityTypeInfo.FromDataEntityType(a_dataEntityReference.EntityType!).CamelCase;
	}

	public ShotGridEntityReference(ShotGridEntityTypeInfo a_entityType, int a_entityId)
	{
		EntityType = a_entityType.CamelCase;
		Id = a_entityId;
	}

	public static ShotGridEntityReference Create<TEntityType>(TEntityType a_target)
		where TEntityType : ShotGridEntity
	{
		return Create(ShotGridEntityTypeInfo.FromType<TEntityType>(), a_target);
	}

	public static ShotGridEntityReference Create(ShotGridEntityTypeInfo a_shotGridTypeTypeInfo, ShotGridEntity a_target)
	{
		ShotGridEntityReference entityRef = new ShotGridEntityReference(a_shotGridTypeTypeInfo, a_target.Id);
		return entityRef;
	}

	public DataEntityReference ToDataEntity()
	{
		if (EntityType == null)
		{
			throw new Exception();
		}

		return new DataEntityReference() { EntityId = Id, EntityType = ShotGridEntityTypeInfo.FromCamelCaseName(EntityType).DataEntityType, EntityName = EntityName};
	}
};