using DataWranglerCommon.IngestDataSources;
using ShotGridIntegration;

namespace DataWranglerCommonTests
{
	public class IngestBlackmagicUrsaTests
	{
		private const string SampleFolder = "../../../../ThirdParty/Blackmagic RAW SDK/Sample/";

		[Fact]
		public void TryMatchBrawFile()
		{
			ShotGridEntityCache entityCache = IngestTests.BuildTestCachedEntries();
			entityCache.AddCachedEntity(new ShotGridEntityShotVersion()
			{
				Attributes = new ShotVersionAttributes()
				{
					DataWranglerMeta = @"{""FileSources"":[{""Source"":"""",""CodecName"":""BlackmagicRAW"",""RecordingStart"": ""2018-04-26T00:00:00.0000000+00:00"",""StartTimeCode"":""22:23:40:20"",""CameraNumber"":""A"",""SourceType"":""BlackmagicUrsa""}]}",
					Description = "This should be the one we link to for this unit test.",
					Flagged = false,
					VersionCode = "test_shot_10"
				},
				Id = 10
			});
			IngestDataCache dataCache = new IngestDataCache();
			dataCache.UpdateCache(entityCache);

			IngestDataSourceResolverBlackmagicUrsa resolver = new IngestDataSourceResolverBlackmagicUrsa();

			List<IngestDataSourceResolver.IngestFileEntry> filesToImport = resolver.ProcessDirectory(SampleFolder, "TEST", entityCache, dataCache);
			Assert.True(filesToImport.Count == 1);
			Assert.True(filesToImport[0].TargetShotVersion.Id == 10);
		}
	}
}
