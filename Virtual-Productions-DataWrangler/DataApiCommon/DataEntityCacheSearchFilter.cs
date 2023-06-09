using DataApiCommon;

namespace DataApiCommon
{
	public class DataEntityCacheSearchFilter
	{
		public readonly Type TargetEntityName;
		private readonly List<DataEntityCacheSearchCondition> m_cacheSearchConditions;

		public DataEntityCacheSearchFilter(Type a_targetEntityName, IReadOnlyList<DataEntityCacheSearchCondition> a_searchConditions)
		{
			TargetEntityName = a_targetEntityName;
			m_cacheSearchConditions = new List<DataEntityCacheSearchCondition>(a_searchConditions.Count);
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
			throw new NotImplementedException();
		}
	}
}
