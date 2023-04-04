
namespace DataWranglerServiceWorker
{
	public interface IFileMetaResolver
	{
		//Loops over directory structure to import files based on their relative path and other parameters and links this to meta.
		public void ProcessDirectory(string a_baseDirectory, string a_storageName, ShotGridDataCache a_cache, DataImportWorker a_importWorker);
	}
}
