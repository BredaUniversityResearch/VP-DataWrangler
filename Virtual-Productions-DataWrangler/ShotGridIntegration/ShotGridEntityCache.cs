using System.Net.NetworkInformation;

namespace ShotGridIntegration
{
	public class ShotGridEntityCache
	{
		private Dictionary<ShotGridEntityName, Dictionary<int, ShotGridEntity>> m_entitiesByNameAndId = new();

		public TEntityType? FindEntity<TEntityType>(int a_entityId)
			where TEntityType : ShotGridEntity
		{
			return (TEntityType?)FindEntity(ShotGridEntityName.FromType<TEntityType>(), a_entityId);
		}

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

		public TEntityType[] FindEntities<TEntityType>(ShotGridSimpleSearchFilter a_searchFilter)
			where TEntityType: ShotGridEntity
		{
			ShotGridEntityName targetName = ShotGridEntityName.FromType<TEntityType>();

			var entitiesById = FindEntitiesByType(targetName);
			if (entitiesById == null)
			{
				return Array.Empty<TEntityType>();
			}

			ShotGridEntityCacheSearchFilter cacheFilter = a_searchFilter.BuildCacheFilter(targetName);

			List<TEntityType> targetEntities = new List<TEntityType>(entitiesById.Count);
			foreach (ShotGridEntity entity in entitiesById.Values)
			{
				if (cacheFilter.Matches(entity))
				{
					targetEntities.Add((TEntityType)entity);
				}
			}

			return targetEntities.ToArray();
		}

		public TEntityType[] GetEntitiesByType<TEntityType>()
			where TEntityType: ShotGridEntity
		{
			var entitiesById = FindEntitiesByType(ShotGridEntityName.FromType<TEntityType>());
			if (entitiesById == null)
			{
				return Array.Empty<TEntityType>();
			}

			int resultIndex = 0;
			TEntityType[] result = new TEntityType[entitiesById.Count];
			foreach(var entity in entitiesById.Values)
			{
				result[resultIndex] = (TEntityType) entity;
				++resultIndex;
			}

			return result;
		}
	}
}
