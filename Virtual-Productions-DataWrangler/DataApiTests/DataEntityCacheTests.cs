using DataApiCommon;

namespace DataApiTests
{
	[TestClass]
	public class DataEntityCacheTests
	{

		[TestMethod]
		public void CacheFilter()
		{
			DataEntityCache cache = new DataEntityCache();
			cache.AddCachedEntity(new DataEntityShot
			{
					EntityId = 10,
					EntityRelationships = new DataEntityRelationships
					{
						Project = new DataEntityReference(typeof(DataEntityProject), 1)
					}
				}
			);

			cache.AddCachedEntity(new DataEntityShot
			{
				EntityId = 11,
					EntityRelationships = new DataEntityRelationships
					{
						Project = new DataEntityReference(typeof(DataEntityProject), 1)
					}
				}
			);

			cache.AddCachedEntity(new DataEntityShot
				{
					EntityId = 12,
					EntityRelationships = new DataEntityRelationships
					{
						Project = new DataEntityReference(typeof(DataEntityProject), 2)
					}
				}
			);

			DataEntityShot[] shots = cache.FindEntities<DataEntityShot>(DataEntityCacheSearchFilter.ForProject(1));
			Assert.IsTrue(shots.Length == 2);
			DataEntityShot[] shotsProject2 = cache.FindEntities<DataEntityShot>(DataEntityCacheSearchFilter.ForProject(2));
			Assert.IsTrue(shotsProject2.Length == 1);
			DataEntityShot[] shotsProject3 = cache.FindEntities<DataEntityShot>(DataEntityCacheSearchFilter.ForProject(3));
			Assert.IsTrue(shotsProject3.Length == 0);
		}
	}
}