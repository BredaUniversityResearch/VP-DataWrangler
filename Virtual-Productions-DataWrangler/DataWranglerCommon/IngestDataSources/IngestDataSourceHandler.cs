namespace DataWranglerCommon.IngestDataSources
{
	public abstract class IngestDataSourceHandler
	{
		public abstract void InstallHooks(DataWranglerEventDelegates a_eventDelegates);
	}
}
