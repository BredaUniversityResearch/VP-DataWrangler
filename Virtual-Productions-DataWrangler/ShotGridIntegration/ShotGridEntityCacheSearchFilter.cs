namespace ShotGridIntegration
{
	public class ShotGridEntityCacheSearchFilter
	{
		public readonly ShotGridEntityName TargetEntityName;
		private readonly List<ShotGridEntityCacheSearchCondition> m_cacheSearchConditions;

		public ShotGridEntityCacheSearchFilter(ShotGridEntityName a_targetEntityName, IReadOnlyList<ShotGridSearchCondition> a_searchConditions)
		{
			TargetEntityName = a_targetEntityName;
			m_cacheSearchConditions = new List<ShotGridEntityCacheSearchCondition>(a_searchConditions.Count);
			BuildLocalSearch(a_searchConditions);
		}

		private void BuildLocalSearch(IReadOnlyList<ShotGridSearchCondition> a_searchConditions)
		{
			for (int i = 0; i < a_searchConditions.Count; ++i)
			{
				m_cacheSearchConditions.Add(new ShotGridEntityCacheSearchCondition(TargetEntityName, a_searchConditions[i]));
			}
		}

		public bool Matches(ShotGridEntity a_entity)
		{
			foreach (ShotGridEntityCacheSearchCondition condition in m_cacheSearchConditions)
			{
				if (!condition.Matches(a_entity))
				{
					return false;
				}
			}

			return true;
		}
	}
}
