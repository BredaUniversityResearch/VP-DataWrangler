using System;
using System.IO;
using CommonLogging;
using DataWranglerCommon;

namespace DataWranglerServiceWorker;

internal class MetaFileResolverVicon : IMetaFileResolver
{
	private static readonly string[] ExtensionsToImport = {""};

	public void ProcessCache(ShotGridDataCache a_metaValues, DataImportWorker a_importWorker)
	{
		var metasToParse = a_metaValues.FindShotVersionWithMeta<DataWranglerFileSourceMetaViconTrackingData>();
		foreach (var currentMeta in metasToParse)
		{
			string targetBasePath = Path.Combine(currentMeta.Key.TempCaptureLibraryPath, currentMeta.Key.TempCaptureFileName);
			foreach(string extension in ExtensionsToImport)
			{
				string pathWithExtension = targetBasePath + extension;
				if (File.Exists(pathWithExtension))
				{
					a_importWorker.AddFileToImport(currentMeta.Value.Identifier, pathWithExtension, currentMeta.Key.SourceFileTag);
				}
				else
				{
					Logger.LogInfo("FileResolverVicon", $"Could not find file \"{pathWithExtension}\" to import");
				}
			}
		}
	}
}