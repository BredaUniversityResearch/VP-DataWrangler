namespace DataWranglerCommon.IngestDataSources
{
	public abstract class IngestDataSourceMeta
	{
		public abstract string SourceType { get; }
		public abstract IngestDataSourceMeta Clone();
	}
}
