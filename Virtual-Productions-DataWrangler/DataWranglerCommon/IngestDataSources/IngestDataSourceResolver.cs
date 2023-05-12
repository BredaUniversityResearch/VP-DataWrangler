namespace DataWranglerCommon.IngestDataSources
{
	public abstract class IngestDataSourceResolver
	{
		public class IngestFileEntry
		{
		};

		public abstract List<IngestFileEntry> ProcessDirectory(string a_baseDirectory, string a_storageName/*, ShotGridDataCache a_cache*/);
	}
}
