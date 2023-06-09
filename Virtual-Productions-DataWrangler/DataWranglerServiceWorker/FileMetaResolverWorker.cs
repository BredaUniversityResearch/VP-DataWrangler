using System;
using System.Collections.Generic;
using System.IO;
using CommonLogging;
using DataApiCommon;
using DataWranglerCommon.IngestDataSources;

namespace DataWranglerServiceWorker
{
	public class FileMetaResolverWorker
	{
		private string m_rootPath;
		private DriveInfo m_targetDriveInfo;
		private DataEntityCache m_cache;
		private IngestDataCache m_ingestCache;
		private DataImportWorker m_importWorker;
		private IngestDataSourceResolverCollection m_fileResolvers;

		public FileMetaResolverWorker(string a_rootPath, DataEntityCache a_cache, DataImportWorker a_importWorker, IngestDataSourceResolverCollection a_fileResolvers, IngestDataCache a_ingestCache)
		{
			Logger.LogInfo("FileMetaResolverWorker", $"Starting file discovery for drive {a_rootPath}");
			m_rootPath = a_rootPath;
			string? rootPath = Path.GetPathRoot(a_rootPath);
			if (rootPath == null)
			{
				throw new ArgumentException($"root path not valid: {a_rootPath}");
			}

			m_targetDriveInfo = new DriveInfo(rootPath);
			m_cache = a_cache;
			m_importWorker = a_importWorker;
			m_fileResolvers = a_fileResolvers;
			m_ingestCache = a_ingestCache;
		}

		public void Run()
		{
			if (!m_targetDriveInfo.IsReady)
			{
				Logger.LogError("FileMetaResolverWorker", $"Failed to import files from {m_rootPath}. Drive is not ready");
				return;
			}

			string storageName = m_targetDriveInfo.VolumeLabel;

			foreach (IngestDataSourceResolver resolver in m_fileResolvers.DataSourceResolvers)
			{
				if (!resolver.CanProcessDirectory)
				{
					continue;
				}

				List<IngestDataSourceResolver.IngestFileEntry> filesToIngest = resolver.ProcessDirectory(m_rootPath, storageName, m_cache, m_ingestCache);

				foreach (IngestDataSourceResolver.IngestFileEntry entry in filesToIngest)
				{
					m_importWorker.AddFileToImport(entry.TargetShotVersion, entry.SourcePath, entry.FileTag);
				}
			}

			Logger.LogInfo("FileMetaResolverWorker", $"Done processing files for path {m_rootPath}");
		}
	}
}
