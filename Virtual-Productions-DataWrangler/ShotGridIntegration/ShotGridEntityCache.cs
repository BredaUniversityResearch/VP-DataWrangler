using System.Net.NetworkInformation;

namespace ShotGridIntegration
{
	public class ShotGridEntityCache
	{
		private Dictionary<ShotGridEntityName, Dictionary<int, ShotGridEntity>> m_entitiesByNameAndId = new();

		public ShotGridEntity? FindEntity(ShotGridEntityName a_entityTypeName, int a_entityId)
		{
			var entitiesById = FindEntitiesByType(a_entityTypeName);
			if (entitiesById != null && entitiesById.TryGetValue(a_entityId, out var resultEntity))
			{
				return resultEntity;
			}

			return null;
		}

		private Dictionary<int, ShotGridEntity>? FindEntitiesByType(ShotGridEntityName a_entityName)
		{
			if (m_entitiesByNameAndId.TryGetValue(a_entityName, out var result))
			{
				return result;
			}

			return null;
		}

		public void AddCachedEntity<TEntityType>(TEntityType a_entity)
			where TEntityType: ShotGridEntity
		{
			AddCachedEntity(a_entity.ShotGridType, a_entity);
		}

		public void AddCachedEntity(ShotGridEntityName a_entityName, ShotGridEntity a_entity)
		{
			var entitiesById = FindEntitiesByType(a_entityName);
			if (entitiesById == null)
			{
				entitiesById = new Dictionary<int, ShotGridEntity>();
				m_entitiesByNameAndId.Add(a_entityName, entitiesById);
			}

			entitiesById[a_entity.Id] = a_entity;
		}

		public TEntityType[] FindEntities<TEntityType>(ShotGridSimpleSearchFilter a_forProject)
			where TEntityType: ShotGridEntity
		{
			var entitiesById = FindEntitiesByType(ShotGridEntityName.FromType<TEntityType>());
			if (entitiesById == null)
			{
				return Array.Empty<TEntityType>();
			}

			List<TEntityType> targetEntities = new List<TEntityType>(entitiesById.Count);
			foreach (ShotGridEntity entity in entitiesById.Values)
			{
			}

			return targetEntities.ToArray();
		}
	}
}
