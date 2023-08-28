using DataApiCommon;
using DataWranglerCommon.IngestDataSources;

namespace DataWranglerCommonTests
{
	public class IngestBlackmagicUrsaTests
	{
		private const string SampleFolder = "../../../../ThirdParty/Blackmagic RAW SDK/Sample/";

		[Fact]
		public void TryMatchBrawFile()
		{
			Guid targetGuid = new Guid(10, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

			DataEntityCache entityCache = IngestTests.BuildTestCachedEntries();
			entityCache.AddCachedEntity(new DataEntityShotVersion()
			{
				DataWranglerMeta = @"{""FileSources"":[{""Source"":"""",""CodecName"":""BlackmagicRAW"",""RecordingStart"": ""2018-04-26T00:00:00.0000000+00:00"",""StartTimeCode"":""22:23:40:20"",""CameraNumber"":""A"",""SourceType"":""BlackmagicUrsa""}]}",
				Description = "This should be the one we link to for this unit test.",
				Flagged = false,
				ShotVersionName = "test_shot_10",
				EntityId = targetGuid
			});
			IngestDataCache dataCache = new IngestDataCache();
			dataCache.UpdateCache(entityCache);

			IngestDataSourceResolverBlackmagicUrsa resolver = new IngestDataSourceResolverBlackmagicUrsa();

			List<IngestFileResolutionDetails> resolutionDetails = resolver.ProcessDirectory(SampleFolder, "TEST", entityCache, dataCache);

			int successCount = 0;
			foreach(IngestFileResolutionDetails details in resolutionDetails)
			{
				if (details.HasSuccessfulResolution())
				{
					++successCount;
					Assert.True(details.TargetShotVersion.EntityId == targetGuid);
				}
			}

			Assert.True(successCount == 1);
		}
	}
}
