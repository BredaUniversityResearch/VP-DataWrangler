using System.IO;
using System.Text;
using CommonLogging;
using DataWranglerCommon;

namespace DataWranglerServiceWorker
{
	public abstract class FileMetaResolver
	{
		public abstract void ProcessDirectory(string a_baseDirectory, string a_storageName, ShotGridDataCache a_cache, DataImportWorker a_importWorker);
	}

	public class FileMetaResolverBlackmagicUrsa: FileMetaResolver
	{
		public override void ProcessDirectory(string a_baseDirectory, string a_storageName, ShotGridDataCache a_cache, DataImportWorker a_importWorker)
		{
			var relevantCacheEntries = a_cache.FindShotVersionWithMeta<DataWranglerFileSourceMetaBlackmagicUrsa>();

			foreach (string filePath in Directory.EnumerateFiles(a_baseDirectory))
			{
				FileInfo fileInfo = new FileInfo(filePath);
				if (CameraCodec.FindFromFileExtension(fileInfo.Extension, out ECameraCodec codec))
				{
					bool fileWasLinked = false;
					StringBuilder rejectionLog = new StringBuilder(256);
					foreach (var cacheEntry in relevantCacheEntries)
					{
						DataWranglerFileSourceMetaBlackmagicUrsa ursaMeta = cacheEntry.Key;
						if (ursaMeta.IsSourceFor(fileInfo, a_storageName, codec.ToString(), out var a_reasonForRejection))
						{
							Logger.LogInfo("FileResolverUrsa", $"Found file {filePath} for shot {cacheEntry.Value.ShotCode} ({cacheEntry.Value.Identifier.VersionId})");

							a_importWorker.AddFileToImport(cacheEntry.Value.Identifier, fileInfo.FullName, ursaMeta.SourceFileTag);
							fileWasLinked = true;
						}
						else
						{
							rejectionLog.AppendLine($"\t{cacheEntry.Value.Identifier.ProjectId}:{cacheEntry.Value.Identifier.ShotId}:{cacheEntry.Value.ShotCode}: {a_reasonForRejection}");
						}
					}
					if (!fileWasLinked)
					{
						Logger.LogInfo("FileResolverUrsa", $"File {filePath} could not be linked to a shot. Rejection log: \n{rejectionLog}");
					}
				}

			}
		}
	}

	public class FileMetaResolverTascam : FileMetaResolver
	{
		public override void ProcessDirectory(string a_baseDirectory, string a_storageName, ShotGridDataCache a_cache, DataImportWorker a_importWorker)
		{
			string sysFilePath = Path.Combine(a_baseDirectory, "dr-1.sys");
			if (!File.Exists(sysFilePath))
			{
				//TASCAM always writes a dr-1.sys file in the root. If this does not exist we can assume that this is not a relevant target.
				Logger.LogInfo("FileDiscoveryWorker", $"TASCAM meta resolver skipping {a_baseDirectory} due to absence of dr-1.sys file ({sysFilePath})");
				return;
			}

			string fileBasePath = Path.Combine(a_baseDirectory, "MUSIC");
			if (!Directory.Exists(fileBasePath))
			{
				Logger.LogInfo("FileDiscoveryWorker", $"TASCAM meta resolver skipping {a_baseDirectory} due to absence of MUSIC folder ({fileBasePath})");
			}

			var relevantCacheEntries = a_cache.FindShotVersionWithMeta<DataWranglerFileSourceMetaTascam>();

			foreach (string filePath in Directory.EnumerateFiles(fileBasePath))
			{
				FileInfo fileInfo = new FileInfo(filePath);
				foreach (var cacheEntry in relevantCacheEntries)
				{
					DataWranglerFileSourceMetaTascam tascamMeta = cacheEntry.Key;
					if (tascamMeta.IsSourceFor(fileInfo, a_storageName))
					{
						Logger.LogInfo("FileDiscoveryWorker", $"TASCAM meta resolver added {filePath} to import queue");
						a_importWorker.AddFileToImport(cacheEntry.Value.Identifier, filePath, tascamMeta.SourceFileTag);
					}
				}
			}
		}
	}
}
