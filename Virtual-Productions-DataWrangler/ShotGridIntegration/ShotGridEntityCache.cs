using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;

namespace ShotGridIntegration
{
	public class ShotGridEntityCache
	{
		private Dictionary<ShotGridEntityTypeInfo, Dictionary<int, ShotGridEntity>> m_entitiesByNameAndId = new();

		public TEntityType? FindEntityById<TEntityType>(int a_entityId)
			where TEntityType : ShotGridEntity
		{
			return (TEntityType?)FindEntityById(ShotGridEntityTypeInfo.FromType<TEntityType>(), a_entityId);
		}

		public ShotGridEntity? FindEntityById(ShotGridEntityTypeInfo a_entityTypeTypeInfo, int a_entityId)
		{
			var entitiesById = FindEntitiesByType(a_entityTypeTypeInfo);
			if (entitiesById != null && entitiesById.TryGetValue(a_entityId, out var resultEntity))
			{
				return resultEntity;
			}

			return null;
		}

		private Dictionary<int, ShotGridEntity>? FindEntitiesByType(ShotGridEntityTypeInfo a_entityTypeInfo)
		{
			if (m_entitiesByNameAndId.TryGetValue(a_entityTypeInfo, out var result))
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

		public void AddCachedEntity(ShotGridEntityTypeInfo a_entityTypeInfo, ShotGridEntity a_entity)
		{
			var entitiesById = FindEntitiesByType(a_entityTypeInfo);
			if (entitiesById == null)
			{
				entitiesById = new Dictionary<int, ShotGridEntity>();
				m_entitiesByNameAndId.Add(a_entityTypeInfo, entitiesById);
			}

			entitiesById[a_entity.Id] = a_entity;
		}

		public TEntityType[] FindEntities<TEntityType>(ShotGridSimpleSearchFilter a_searchFilter)
			where TEntityType: ShotGridEntity
		{
			ShotGridEntityTypeInfo targetTypeInfo = ShotGridEntityTypeInfo.FromType<TEntityType>();

			var entitiesById = FindEntitiesByType(targetTypeInfo);
			if (entitiesById == null)
			{
				return Array.Empty<TEntityType>();
			}

			ShotGridEntityCacheSearchFilter cacheFilter = a_searchFilter.BuildCacheFilter(targetTypeInfo);

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
			var entitiesById = FindEntitiesByType(ShotGridEntityTypeInfo.FromType<TEntityType>());
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

		public bool TryGetEntityById<TEntityType>(int a_entityId, [NotNullWhen(true)] out TEntityType? a_result)
			where TEntityType: ShotGridEntity
		{
			a_result = FindEntityById<TEntityType>(a_entityId);
			return a_result != null;
		}

		public ShotGridEntity? FindEntityByPredicate(ShotGridEntityTypeInfo a_entityTypeTypeInfo, Func<ShotGridEntity, bool> a_func)
		{
			var entities = FindEntitiesByType(a_entityTypeTypeInfo);
			if (entities == null)
			{
				return null;
			}

			foreach(var entity in entities.Values)
			{
				if (a_func(entity))
				{
					return entity;
				}
			}

			return null;
		}

		public TEntityType? FindEntityByPredicate<TEntityType>(Func<TEntityType, bool> a_predicate)
			where TEntityType: ShotGridEntity
		{
			return (TEntityType?)FindEntityByPredicate(ShotGridEntityTypeInfo.FromType<TEntityType>(), a_ent => a_predicate((TEntityType)a_ent));
		}

		public bool TryGetEntityByPredicate(ShotGridEntityTypeInfo a_entityType, Func<ShotGridEntity, bool> a_func, [NotNullWhen(true)] out ShotGridEntity? a_result)
		{
			a_result = FindEntityByPredicate(a_entityType, a_func);
			return a_result != null;
		}

		public bool TryGetEntityByPredicate<TEntityType>(Func<TEntityType, bool> a_func, [NotNullWhen(true)] out TEntityType? a_result)
			where TEntityType: ShotGridEntity
		{
			a_result = FindEntityByPredicate(a_func);
			return a_result != null;
		}
	}
}
