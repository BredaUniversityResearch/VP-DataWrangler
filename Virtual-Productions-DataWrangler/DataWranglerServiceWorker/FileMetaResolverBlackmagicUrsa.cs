using System.IO;
using System.Text;
using CommonLogging;
using DataWranglerCommon;
using DataWranglerCommon.BRAWSupport;

namespace DataWranglerServiceWorker;

public class FileMetaResolverBlackmagicUrsa: FileMetaResolver
{
	public override void ProcessDirectory(string a_baseDirectory, string a_storageName, ShotGridDataCache a_cache, DataImportWorker a_importWorker)
	{
		var relevantCacheEntries = a_cache.FindShotVersionWithMeta<DataWranglerFileSourceMetaBlackmagicUrsa>();
		using BRAWFileDecoder fileDecoder = new BRAWFileDecoder();
		
		foreach (string filePath in Directory.EnumerateFiles(a_baseDirectory))
		{
			FileInfo fileInfo = new FileInfo(filePath);
			if (CameraCodec.FindFromFileExtension(fileInfo.Extension, out ECameraCodec codec))
			{
				TimeCode firstFrameTimeCode = new();
				if (codec == ECameraCodec.BlackmagicRAW)
				{
					firstFrameTimeCode = fileDecoder.GetTimeCodeFromFile(fileInfo);
				}

				bool fileWasLinked = false;
				StringBuilder rejectionLog = new StringBuilder(256);
				foreach (var cacheEntry in relevantCacheEntries)
				{
					DataWranglerFileSourceMetaBlackmagicUrsa ursaMeta = cacheEntry.Key;
					if (ursaMeta.IsSourceFor(fileInfo, a_storageName, codec.ToString(), firstFrameTimeCode, out var reasonForRejection))
					{
						Logger.LogInfo("FileResolverUrsa", $"Found file {filePath} for shot {cacheEntry.Value.ShotCode} ({cacheEntry.Value.Identifier.VersionId})");

						a_importWorker.AddFileToImport(cacheEntry.Value.Identifier, fileInfo.FullName, ursaMeta.SourceFileTag);
						fileWasLinked = true;
					}
					else
					{
						rejectionLog.AppendLine($"\t{cacheEntry.Value.Identifier.ProjectId}:{cacheEntry.Value.Identifier.ShotId}:{cacheEntry.Value.ShotCode}: {reasonForRejection}");
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