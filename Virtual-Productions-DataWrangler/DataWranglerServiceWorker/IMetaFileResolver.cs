using DataWranglerCommon;

namespace DataWranglerServiceWorker
{
	public interface IMetaFileResolver
	{
		//Processes meta and looks for files to import with meta as a base.
		public void ProcessCache(ShotGridDataCache a_metaValues, DataImportWorker a_importWorker);
	}
}
