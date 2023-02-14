using ShotGridIntegration;

namespace DataWranglerServiceWorker
{
	internal class ServiceWorkerConfig
	{
		public static ServiceWorkerConfig Instance { get; }

		public string FilePublishDefaultStatus = ShotGridStatusListEntry.PendingReview;
		public string FilePublishDescription = "File auto-published by Data Wrangler";

		public string DefaultDataStorageName = "CradleNas";
		public string DefaultDataStoreFilePath = "${ProjectName}/RawFootage/${ShotCode}/${ShotVersionCode}/"; //Relative to DataStoreRoot
		public int DefaultCopyBufferSize = 32 * 1024 * 1024;

		static ServiceWorkerConfig()
		{
			Instance = new ServiceWorkerConfig();
		}
	}
}
