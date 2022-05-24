using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataWranglerCommon;

namespace DataWranglerServiceWorker
{
	public class FileDiscoveryWorker
	{
		private string m_rootPath;
		private DriveInfo m_targetDriveInfo;
		private ShotGridDataCache m_cache;
		private DataImportWorker m_importWorker;

		public FileDiscoveryWorker(string a_rootPath, ShotGridDataCache a_cache, DataImportWorker a_importWorker)
		{
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
				Logger.LogError("FileDiscoveryWorker", $"Failed to import files from {m_rootPath}. Drive is not ready");
				return;
			}

			string storageName = m_targetDriveInfo.VolumeLabel;

			foreach (string filePath in Directory.EnumerateFiles(m_rootPath))
			{
				FileInfo fileInfo = new FileInfo(filePath);
				if (CameraCodec.FindFromFileExtension(fileInfo.Extension, out var codec))
				{
					if (m_cache.FindShotVersionForFile(fileInfo.CreationTimeUtc, storageName, codec, 
						    out ShotGridDataCache.ShotVersionMetaCacheEntry? cacheEntry))
					{
						Console.WriteLine($"Found file {filePath} for shot {cacheEntry.ShotCode} ({cacheEntry.Identifier.VersionId})");

						m_importWorker.AddFileToImport(cacheEntry.Identifier, fileInfo.FullName);
					}
				}
			}
		}
	}
}
