using ShotGridIntegration;

namespace DataWranglerCommon.IngestDataSources
{
	public abstract class IngestDataSourceResolver
	{
		public class IngestFileEntry
		{
			public readonly ShotGridEntityShotVersion TargetShotVersion;
			public readonly string SourcePath;
			public readonly string FileTag;

			public IngestFileEntry(ShotGridEntityShotVersion a_targetShotVersion, string a_sourcePath, string a_fileTag)
			{
				TargetShotVersion = a_targetShotVersion;
				SourcePath = a_sourcePath;
				FileTag = a_fileTag;
			}
		};

		public virtual bool CanProcessDirectory => false;
		public virtual bool CanProcessCache => false;

		public virtual List<IngestFileEntry> ProcessDirectory(string a_baseDirectory, string a_storageVolumeName, ShotGridEntityCache a_cache, IngestDataCache a_ingestCache)
		{
			throw new NotImplementedException();
		}

		public virtual List<IngestFileEntry> ProcessCache(ShotGridEntityCache a_cache, IngestDataCache a_ingestCache)
		{
			throw new NotImplementedException();
		}
	}
}
