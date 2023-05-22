using System;
using System.IO;
using CommonLogging;
using DataWranglerCommon;

namespace DataWranglerServiceWorker;

public class FileMetaResolverTascam : IFileMetaResolver
{
	public void ProcessDirectory(string a_baseDirectory, string a_storageName, ShotGridDataCache a_cache, DataImportWorker a_importWorker)
	{
		throw new NotImplementedException(); //TODO

		//string sysFilePath = Path.Combine(a_baseDirectory, "dr-1.sys");
		//if (!File.Exists(sysFilePath))
		//{
		//	//TASCAM always writes a dr-1.sys file in the root. If this does not exist we can assume that this is not a relevant target.
		//	Logger.LogInfo("FileMetaResolverWorker", $"TASCAM meta resolver skipping {a_baseDirectory} due to absence of dr-1.sys file ({sysFilePath})");
		//	return;
		//}

		//string fileBasePath = Path.Combine(a_baseDirectory, "MUSIC");
		//if (!Directory.Exists(fileBasePath))
		//{
		//	Logger.LogInfo("FileMetaResolverWorker", $"TASCAM meta resolver skipping {a_baseDirectory} due to absence of MUSIC folder ({fileBasePath})");
		//}

		//var relevantCacheEntries = a_cache.FindShotVersionWithMeta<DataWranglerFileSourceMetaTascam>();

		//foreach (string filePath in Directory.EnumerateFiles(fileBasePath))
		//{
		//	FileInfo fileInfo = new FileInfo(filePath);
		//	foreach (var cacheEntry in relevantCacheEntries)
		//	{
		//		DataWranglerFileSourceMetaTascam tascamMeta = cacheEntry.Key;
		//		if (tascamMeta.IsSourceFor(fileInfo, a_storageName))
		//		{
		//			Logger.LogInfo("FileMetaResolverWorker", $"TASCAM meta resolver added {filePath} to import queue");
		//			a_importWorker.AddFileToImport(cacheEntry.Value.Identifier, filePath, tascamMeta.SourceFileTag);
		//		}
		//	}
		//}
	}
}