namespace DataWranglerServiceWorker
{
	public abstract class FileMetaResolver
	{
		public abstract void ProcessDirectory(string a_baseDirectory, string a_storageName, ShotGridDataCache a_cache, DataImportWorker a_importWorker);
	}
}
