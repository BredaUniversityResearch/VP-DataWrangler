using DataApiCommon;

namespace DataApiCommon
{
	public class DataEntityCacheSearchFilter
	{
		private readonly List<DataEntityCacheSearchCondition> m_cacheSearchConditions;

		public DataEntityCacheSearchFilter(IReadOnlyList<DataEntityCacheSearchCondition> a_searchConditions)
		{
			m_cacheSearchConditions = new List<DataEntityCacheSearchCondition>(a_searchConditions);
		}

		public bool Matches(DataEntityBase a_entity)
		{
			foreach (DataEntityCacheSearchCondition condition in m_cacheSearchConditions)
			{
				if (!condition.Matches(a_entity))
				{
					return false;
				}
			}

			return true;
		}

		public static DataEntityCacheSearchFilter ForProject(int a_targetProjectId)
		{
			DataEntityCacheSearchFilter filter = new DataEntityCacheSearchFilter(new[] { new DataEntityCacheSearchCondition((a_entity) => a_entity.EntityRelationships.Project?.EntityId == a_targetProjectId)});
			return filter;
		}
	}
}
