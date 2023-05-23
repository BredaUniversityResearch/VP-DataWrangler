using System;
using System.IO;
using CommonLogging;
using DataWranglerCommon;

namespace DataWranglerServiceWorker;

internal class MetaFileResolverVicon : IMetaFileResolver
{
	public void ProcessCache(ShotGridDataCache a_metaValues, DataImportWorker a_importWorker)
	{
		throw new NotImplementedException(); //TODO FIXME
	//	var metasToParse = a_metaValues.FindShotVersionWithMeta<DataWranglerFileSourceMetaViconTrackingData>();
	//	foreach (var currentMeta in metasToParse)
	//	{
	//		if (string.IsNullOrEmpty(currentMeta.Key.TempCaptureFileName) ||
	//			string.IsNullOrEmpty(currentMeta.Key.TempCaptureLibraryPath))
	//		{
	//			Logger.LogError("MetaFileResolverVicon", $"Shot meta specifies empty temp file name or empty capture library on shot {currentMeta.Value.Identifier}");
	//			continue;
	//		}

	//		int matchedCount = 0;
	//		if (Directory.Exists(currentMeta.Key.TempCaptureLibraryPath))
	//		{
	//			foreach (string filePaths in Directory.GetFiles(currentMeta.Key.TempCaptureLibraryPath))
	//			{
	//				FileInfo targetFile = new FileInfo(filePaths);
	//				if (targetFile.Name.StartsWith(currentMeta.Key.TempCaptureFileName))
	//				{
	//					a_importWorker.AddFileToImport(currentMeta.Value.Identifier, targetFile.FullName, currentMeta.Key.SourceFileTag);
	//					++matchedCount;
	//				}
	//			}
	//		}

	//		if (matchedCount == 0)
	//		{
	//			Logger.LogError("MetaFileResolverVicon", $"Failed to find any files to import for meta {currentMeta.Value.Identifier} " +
	//				$"at directory {currentMeta.Key.TempCaptureLibraryPath} with name {currentMeta.Key.TempCaptureFileName}");
	//		}
	//	}
	}
}