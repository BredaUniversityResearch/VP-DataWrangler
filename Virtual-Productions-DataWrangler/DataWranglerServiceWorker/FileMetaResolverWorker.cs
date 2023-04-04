using System;
using System.IO;
using CommonLogging;

namespace DataWranglerServiceWorker
{
	public class FileMetaResolverWorker
	{
		private string m_rootPath;
		private DriveInfo m_targetDriveInfo;
		private ShotGridDataCache m_cache;
		private DataImportWorker m_importWorker;
		private IFileMetaResolver[] m_metaResolvers = { 
			new FileMetaResolverBlackmagicUrsa(), 
			new FileMetaResolverTascam() 
		};

		public FileMetaResolverWorker(string a_rootPath, ShotGridDataCache a_cache, DataImportWorker a_importWorker)
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
		}

		public void Run()
		{
			if (!m_targetDriveInfo.IsReady)
			{
				Logger.LogError("FileMetaResolverWorker", $"Failed to import files from {m_rootPath}. Drive is not ready");
				return;
			}

			string storageName = m_targetDriveInfo.VolumeLabel;

			foreach (IFileMetaResolver resolver in m_metaResolvers)
			{
				resolver.ProcessDirectory(m_rootPath, storageName, m_cache, m_importWorker);
			}

			Logger.LogInfo("FileMetaResolverWorker", $"Done processing files for path {m_rootPath}");
		}
	}
}
