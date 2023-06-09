using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShotGridIntegration;

namespace ShotGridIntegrationTest
{
	[TestClass]
	public class ShotGridEntityCacheTests
	{

		[TestMethod]
		public void CacheFilter()
		{
			ShotGridEntityCache cache = new ShotGridEntityCache();
			cache.AddCachedEntity(new ShotGridEntityShot
				{
					Id = 10,
					EntityRelationships = new ShotGridEntityRelationships
					{
						Project = new ShotGridEntityReference(ShotGridEntityTypeInfo.Project, 1)
					}
				}
			);

			cache.AddCachedEntity(new ShotGridEntityShot
				{
					Id = 11,
					EntityRelationships = new ShotGridEntityRelationships
					{
						Project = new ShotGridEntityReference(ShotGridEntityTypeInfo.Project, 1)
					}
				}
			);

			cache.AddCachedEntity(new ShotGridEntityShot
				{
					Id = 12,
					EntityRelationships = new ShotGridEntityRelationships
					{
						Project = new ShotGridEntityReference(ShotGridEntityTypeInfo.Project, 2)
					}
				}
			);

			ShotGridEntityShot[] shots = cache.FindEntities<ShotGridEntityShot>(ShotGridSimpleSearchFilter.ForProject(1));
			Assert.IsTrue(shots.Length == 2);
			ShotGridEntityShot[] shotsProject2 = cache.FindEntities<ShotGridEntityShot>(ShotGridSimpleSearchFilter.ForProject(2));
			Assert.IsTrue(shotsProject2.Length == 1);
			ShotGridEntityShot[] shotsProject3 = cache.FindEntities<ShotGridEntityShot>(ShotGridSimpleSearchFilter.ForProject(3));
			Assert.IsTrue(shotsProject3.Length == 0);
		}
	}
}