using DataApiCommon;
using DataWranglerCommon.IngestDataSources;

namespace DataWranglerCommonTests
{
	public class IngestTests
	{
		public static DataEntityCache BuildTestCachedEntries()
		{
			var cache = new DataEntityCache();
			cache.AddCachedEntity(new DataEntityShotVersion()
			{
				DataWranglerMeta = @"{""FileSources"":[{""Source"":"""",""CodecName"":""BlackmagicRAW"",""RecordingStart"":""2023-05-11T11:14:10.2834901+00:00"",""StartTimeCode"":""12:08:43:02"",""StorageTarget"":"""",""CameraNumber"":""A"",""SourceType"":""BlackmagicUrsa"",""SourceFileTag"":""video"",""IsUniqueMeta"":true}]}",
				Description = "UNIT_TEST_DATA",
				Flagged = false,
				ShotVersionName = "test_shot_01",
				EntityId = new Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
			});
			cache.AddCachedEntity(new DataEntityShotVersion()
			{
				DataWranglerMeta = "Some invalid JSON data",
				Description = "UNIT_TEST_DATA",
				Flagged = false,
				ShotVersionName = "test_shot_02",
				EntityId = new Guid(2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
			});

			cache.AddCachedEntity(new DataEntityShotVersion()
			{
				DataWranglerMeta = @"{""Dummy"": ""Bogus data""}",
				Description = "UNIT_TEST_DATA",
				Flagged = false,
				ShotVersionName = "test_shot_03",
				EntityId = new Guid(3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)
			});

			return cache;
		}

		[Fact]
		public void BuildCache()
		{
			DataEntityCache entityCache = BuildTestCachedEntries();
			IngestDataCache cache = new IngestDataCache();
			cache.UpdateCache(entityCache);

			var metas = cache.FindShotVersionsWithMeta<IngestDataSourceMetaBlackmagicUrsa>();
			Assert.True(metas.Count == 1);
		}
	}
}
