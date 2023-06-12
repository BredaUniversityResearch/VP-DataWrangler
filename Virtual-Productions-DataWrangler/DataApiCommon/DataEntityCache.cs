using System.Diagnostics.CodeAnalysis;

namespace DataApiCommon
{
	public class DataEntityCache
	{
		private Dictionary<Type, Dictionary<Guid, DataEntityBase>> m_entitiesByNameAndId = new();

		public TEntityType? FindEntityById<TEntityType>(Guid a_entityId)
			where TEntityType : DataEntityBase
		{
			return (TEntityType?)FindEntityById(typeof(TEntityType), a_entityId);
		}

		public DataEntityBase? FindEntityById(Type a_entityTypeName, Guid a_entityId)
		{
			var entitiesById = FindEntitiesByType(a_entityTypeName);
			if (entitiesById != null && entitiesById.TryGetValue(a_entityId, out var resultEntity))
			{
				return resultEntity;
			}

			return null;
		}

		private Dictionary<Guid, DataEntityBase>? FindEntitiesByType(Type a_entityName)
		{
			if (m_entitiesByNameAndId.TryGetValue(a_entityName, out var result))
			{
				return result;
			}

			return null;
		}

		public void AddCachedEntity<TEntityType>(TEntityType a_entity)
			where TEntityType : DataEntityBase
		{
			AddCachedEntity(a_entity.GetType(), a_entity);
		}

		public void AddCachedEntity(Type a_entityName, DataEntityBase a_entity)
		{
			var entitiesById = FindEntitiesByType(a_entityName);
			if (entitiesById == null)
			{
				entitiesById = new Dictionary<Guid, DataEntityBase>();
				m_entitiesByNameAndId.Add(a_entityName, entitiesById);
			}

			entitiesById[a_entity.EntityId] = a_entity;
		}

		public TEntityType[] FindEntities<TEntityType>(DataEntityCacheSearchFilter a_searchFilter)
			where TEntityType : DataEntityBase
		{
			Type targetName = typeof(TEntityType);

			var entitiesById = FindEntitiesByType(targetName);
			if (entitiesById == null)
			{
				return Array.Empty<TEntityType>();
			}

			List<TEntityType> targetEntities = new List<TEntityType>(entitiesById.Count);
			foreach (DataEntityBase entity in entitiesById.Values)
			{
				if (a_searchFilter.Matches(entity))
				{
					targetEntities.Add((TEntityType)entity);
				}
			}

			return targetEntities.ToArray();
		}

		public TEntityType[] GetEntitiesByType<TEntityType>()
			where TEntityType : DataEntityBase
		{
			var entitiesById = FindEntitiesByType(typeof(TEntityType));
			if (entitiesById == null)
			{
				return Array.Empty<TEntityType>();
			}

			int resultIndex = 0;
			TEntityType[] result = new TEntityType[entitiesById.Count];
			foreach (var entity in entitiesById.Values)
			{
				result[resultIndex] = (TEntityType)entity;
				++resultIndex;
			}

			return result;
		}

		public bool TryGetEntityById<TEntityType>(Guid a_entityId, [NotNullWhen(true)] out TEntityType? a_result)
			where TEntityType : DataEntityBase
		{
			a_result = FindEntityById<TEntityType>(a_entityId);
			return a_result != null;
		}

		public DataEntityBase? FindEntityByPredicate(Type a_entityTypeName, Func<DataEntityBase, bool> a_func)
		{
			var entities = FindEntitiesByType(a_entityTypeName);
			if (entities == null)
			{
				return null;
			}

			foreach (var entity in entities.Values)
			{
				if (a_func(entity))
				{
					return entity;
				}
			}

			return null;
		}

		public TEntityType? FindEntityByPredicate<TEntityType>(Func<TEntityType, bool> a_predicate)
			where TEntityType : DataEntityBase
		{
			return (TEntityType?)FindEntityByPredicate(typeof(TEntityType), a_ent => a_predicate((TEntityType)a_ent));
		}

		public bool TryGetEntityByPredicate(Type a_entityType, Func<DataEntityBase, bool> a_func, [NotNullWhen(true)] out DataEntityBase? a_result)
		{
			a_result = FindEntityByPredicate(a_entityType, a_func);
			return a_result != null;
		}

		public bool TryGetEntityByPredicate<TEntityType>(Func<TEntityType, bool> a_func, [NotNullWhen(true)] out TEntityType? a_result)
			where TEntityType : DataEntityBase
		{
			a_result = FindEntityByPredicate(a_func);
			return a_result != null;
		}
	}
}
