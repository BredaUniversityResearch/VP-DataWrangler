using DataApiCommon;

namespace DataApiTests
{
	[TestClass]
	public class DataEntityCacheTests
	{

		[TestMethod]
		public void CacheFilter()
		{
			Guid project1 = new Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
			Guid project2 = new Guid(2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
			Guid project3 = new Guid(3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

			DataEntityCache cache = new DataEntityCache();
			cache.AddCachedEntity(new DataEntityShot
			{
					EntityId = new Guid(10, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
					EntityRelationships = new DataEntityRelationships
					{
						Project = new DataEntityReference(typeof(DataEntityProject), project1)
					}
				}
			);

			cache.AddCachedEntity(new DataEntityShot
			{
				EntityId = new Guid(11, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
					EntityRelationships = new DataEntityRelationships
					{
						Project = new DataEntityReference(typeof(DataEntityProject), project1)
					}
				}
			);

			cache.AddCachedEntity(new DataEntityShot
				{
					EntityId = new Guid(12, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0),
					EntityRelationships = new DataEntityRelationships
					{
						Project = new DataEntityReference(typeof(DataEntityProject), project2)
					}
				}
			);

			DataEntityShot[] shots = cache.FindEntities<DataEntityShot>(DataEntityCacheSearchFilter.ForProject(project1));
			Assert.IsTrue(shots.Length == 2);
			DataEntityShot[] shotsProject2 = cache.FindEntities<DataEntityShot>(DataEntityCacheSearchFilter.ForProject(project2));
			Assert.IsTrue(shotsProject2.Length == 1);
			DataEntityShot[] shotsProject3 = cache.FindEntities<DataEntityShot>(DataEntityCacheSearchFilter.ForProject(project3));
			Assert.IsTrue(shotsProject3.Length == 0);
		}
	}
}