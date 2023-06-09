using System.IO;
using CommonLogging;
using Renci.SshNet;
using Renci.SshNet.Common;
using ShotGridIntegration;

namespace DataWranglerServiceWorker
{
	internal class ServiceWorkerConfig
	{
		public static ServiceWorkerConfig Instance { get; }

		public string FilePublishDefaultStatus = ShotGridStatusListEntry.PendingReview;
		public string FilePublishDescription = "File auto-published by Data Wrangler";

		public string DefaultDataStorageName = "CradleNas";
		public string DefaultDataStoreFilePath = "${ProjectName}/RawFootage/${ShotName}/${ShotVersionCode}/"; //Relative to DataStoreRoot

		public string DefaultDataStoreFtpHost = "cradlenas";	//Host for the sFTP file publisher.
		public string DefaultDataStoreFtpUserName = "nas";
		public string DefaultDataStoreFtpRelativeRoot = "/projects/VirtualProductions/"; //What folder in sFTP should we publish to? 
		public string DefaultDataStoreFtpKeyFilePath = "nas.pem";
		public PrivateKeyFile? DefaultDataStoreFtpKeyFile;

		static ServiceWorkerConfig()
		{
			Instance = new ServiceWorkerConfig();
		}

		private ServiceWorkerConfig()
		{
			if (!TryReloadPrivateKey())
			{
				Logger.LogError("Config", $"Failed to acquire ftp private key, file \"{DefaultDataStoreFtpKeyFilePath}\" not found.");
			}
		}

		public bool TryReloadPrivateKey()
		{
			if (File.Exists(DefaultDataStoreFtpKeyFilePath))
			{
				try
				{
					DefaultDataStoreFtpKeyFile = new PrivateKeyFile(DefaultDataStoreFtpKeyFilePath);
				}
				catch (SshException ex)
				{
					Logger.LogError("Config", $"Failed to load private key file. Exception: {ex.Message}");
				}

				return DefaultDataStoreFtpKeyFile != null;
			}

			return false;
		}
	}
}
