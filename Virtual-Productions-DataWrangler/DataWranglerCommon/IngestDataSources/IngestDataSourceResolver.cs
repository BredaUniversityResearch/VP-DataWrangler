using DataApiCommon;

namespace DataWranglerCommon.IngestDataSources
{
	public abstract class IngestDataSourceResolver
	{
		public class IngestFileEntry
		{
			public readonly DataEntityShotVersion TargetShotVersion;
			public readonly string SourcePath;
			public readonly string FileTag;

			public IngestFileEntry(DataEntityShotVersion a_targetShotVersion, string a_sourcePath, string a_fileTag)
			{
				TargetShotVersion = a_targetShotVersion;
				SourcePath = a_sourcePath;
				FileTag = a_fileTag;
			}
		};

		public readonly bool CanProcessDirectory;
		public readonly bool CanProcessCache;

		protected IngestDataSourceResolver(bool a_processDirectory, bool a_processCache)
		{
			CanProcessDirectory = a_processDirectory;
			CanProcessCache = a_processCache;
		}

		public virtual List<IngestFileEntry> ProcessDirectory(string a_baseDirectory, string a_storageVolumeName, DataEntityCache a_cache, IngestDataCache a_ingestCache, List<IngestFileResolutionDetails> a_fileResolutionDetails)
		{
			throw new NotImplementedException();
		}

		public virtual List<IngestFileEntry> ProcessCache(DataEntityCache a_cache, IngestDataCache a_ingestCache)
		{
			throw new NotImplementedException();
		}
	}
}
