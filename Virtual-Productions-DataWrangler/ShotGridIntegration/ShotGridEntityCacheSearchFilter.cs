namespace ShotGridIntegration
{
	public class ShotGridEntityCacheSearchFilter
	{
		public readonly ShotGridEntityTypeInfo TargetEntityTypeInfo;
		private readonly List<ShotGridEntityCacheSearchCondition> m_cacheSearchConditions;

		public ShotGridEntityCacheSearchFilter(ShotGridEntityTypeInfo a_targetEntityTypeInfo, IReadOnlyList<ShotGridSearchCondition> a_searchConditions)
		{
			TargetEntityTypeInfo = a_targetEntityTypeInfo;
			m_cacheSearchConditions = new List<ShotGridEntityCacheSearchCondition>(a_searchConditions.Count);
			BuildLocalSearch(a_searchConditions);
		}

		private void BuildLocalSearch(IReadOnlyList<ShotGridSearchCondition> a_searchConditions)
		{
			for (int i = 0; i < a_searchConditions.Count; ++i)
			{
				m_cacheSearchConditions.Add(new ShotGridEntityCacheSearchCondition(TargetEntityTypeInfo, a_searchConditions[i]));
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
