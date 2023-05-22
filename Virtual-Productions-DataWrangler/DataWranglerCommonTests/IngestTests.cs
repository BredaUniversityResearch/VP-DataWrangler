using DataWranglerCommon.IngestDataSources;
using ShotGridIntegration;

namespace DataWranglerCommonTests
{
	public class IngestTests
	{
		public static ShotGridEntityCache BuildTestCachedEntries()
		{
			var cache = new ShotGridEntityCache();
			cache.AddCachedEntity(new ShotGridEntityShotVersion()
			{
				Attributes = new ShotVersionAttributes() { 
					DataWranglerMeta = @"{""FileSources"":[{""Source"":"""",""CodecName"":""BlackmagicRAW"",""RecordingStart"":""2023-05-11T11:14:10.2834901+00:00"",""StartTimeCode"":""12:08:43:02"",""StorageTarget"":"""",""CameraNumber"":""A"",""SourceType"":""BlackmagicUrsa"",""SourceFileTag"":""video"",""IsUniqueMeta"":true}]}",
					Description = "UNIT_TEST_DATA",
					Flagged = false,
					VersionCode = "test_shot_01"
				},
				Id = 1
			});
			cache.AddCachedEntity(new ShotGridEntityShotVersion()
			{
				Attributes = new ShotVersionAttributes()
				{
					DataWranglerMeta = "Some invalid JSON data",
					Description = "UNIT_TEST_DATA",
					Flagged = false,
					VersionCode = "test_shot_02"
				},
				Id = 2
			});

			cache.AddCachedEntity(new ShotGridEntityShotVersion()
			{
				Attributes = new ShotVersionAttributes()
				{
					DataWranglerMeta = @"{""Dummy"": ""Bogus data""}",
					Description = "UNIT_TEST_DATA",
					Flagged = false,
					VersionCode = "test_shot_03"
				},
				Id = 3
			});

			return cache;
		}

		[Fact]
		public void BuildCache()
		{
			ShotGridEntityCache entityCache = BuildTestCachedEntries();
			IngestDataCache cache = new IngestDataCache();
			cache.UpdateCache(entityCache);

			var metas = cache.FindShotVersionsWithMeta<IngestDataSourceMetaBlackmagicUrsa>();
			Assert.True(metas.Count == 1);
		}
	}
}
