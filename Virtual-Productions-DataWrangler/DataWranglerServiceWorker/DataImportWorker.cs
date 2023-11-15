using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using CommonLogging;
using DataApiCommon;
using DataWranglerCommon;
using Newtonsoft.Json;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
using ShotGridIntegration;

namespace DataWranglerServiceWorker
{
	public class DataImportWorker
	{
		public enum ECopyResult
		{
			Success, //Did copy the file
			UnknownFailure, //Something happened, not sure what.
			FileAlreadyUpToDate, //File already exists at target and did not need update.
			InvalidDestinationPath, //Destination path is invalid, could not resolve the folder for the destination
		};

		private class ImportQueueEntry
		{
			public FileCopyMetaData CopyMetaData;
			public DataEntityShotVersion TargetShotVersion;

			public ImportQueueEntry(FileCopyMetaData a_copyMetaData, DataEntityShotVersion a_shotVersion)
			{
				CopyMetaData = a_copyMetaData;
				TargetShotVersion = a_shotVersion;
			}
		};

		public delegate void CopyStartedDelegate(DataEntityShotVersion shotVersion, FileCopyMetaData metaData);
		public delegate void CopyProgressUpdate(DataEntityShotVersion shotVersion, FileCopyMetaData metaData, FileCopyProgress progress);
		public delegate void CopyWriteMetaDataStart(DataEntityShotVersion shotVersion, FileCopyMetaData metaData);
		public delegate void CopyFinishedDelegate(DataEntityShotVersion shotVersion, FileCopyMetaData metaData, ECopyResult result);

		public event CopyStartedDelegate OnCopyStarted = delegate { };
		public event CopyProgressUpdate OnCopyUpdate = delegate { };
		public event CopyWriteMetaDataStart OnCopyStartWriteMetaData = delegate { };
		public event CopyFinishedDelegate OnCopyFinished = delegate { };

		private DataApi m_api;
		private DataEntityCache m_localCache;
		private Queue<ImportQueueEntry> m_importQueue = new();
		public int ImportQueueLength => m_importQueue.Count;
		private AutoResetEvent m_queueAddedEvent = new AutoResetEvent(false);
		private Thread m_dataImportThread;
		private CancellationTokenSource m_dataImportThreadCancellationToken = new CancellationTokenSource();

		private SftpClient? m_importClient = null;

		public DataImportWorker(DataApi a_api)
		{
			m_api = a_api;
			m_localCache = m_api.LocalCache;
			m_dataImportThread = new Thread(DoBackgroundWork);
		}

		public void Start()
		{
			m_dataImportThread.Start();
		}

		public void Stop()
		{
			m_dataImportThreadCancellationToken.Cancel();
			m_dataImportThread.Join();
		}

		public void AddFileToImport(DataEntityShotVersion a_shotVersion, string a_sourceFilePath, string a_fileTag)
		{
			if (!m_localCache.TryGetEntityByPredicate(a_obj => a_obj.LocalStorageName == ServiceWorkerConfig.Instance.DefaultDataStorageName, out DataEntityLocalStorage? targetStorage) ||
			    targetStorage.StorageRoot == null)
			{
				Logger.LogError("DataImport", $"Could not import file at path {a_sourceFilePath}. Could not find data storage with name \"{ServiceWorkerConfig.Instance.DefaultDataStorageName}\". Importer is NOT active");
				return;
			}

			if (a_shotVersion.EntityRelationships.Project == null)
			{
				Logger.LogError("DataImport", $"Could not import file at path {a_sourceFilePath}. ShotVersion meta is incomplete, Relationships.Project is null");
				return;
			}

			if (a_shotVersion.EntityRelationships.Parent == null || a_shotVersion.EntityRelationships.Parent.EntityType != typeof(DataEntityShot))
			{
				Logger.LogError("DataImport", $"Could not import file at path {a_sourceFilePath}. ShotVersion meta is incomplete, Relationships.Parent is null or not of expected type ({typeof(DataEntityShot)} Got: {a_shotVersion.EntityRelationships.Parent?.EntityType?.ToString()??"NULL"}");
				return;
			}

			if (!m_localCache.TryGetEntityById(a_shotVersion.EntityRelationships.Project.EntityId, out DataEntityProject? project))
			{
				Logger.LogError("DataImport", $"Could not import file at path {a_sourceFilePath}. Data references project ({a_shotVersion.EntityRelationships.Project.EntityId}) which is not known by the cache");
				return;
			}

			if (!m_localCache.TryGetEntityById(a_shotVersion.EntityRelationships.Parent.EntityId, out DataEntityShot? shot))
			{
				Logger.LogError("DataImport", $"Could not import file at path {a_sourceFilePath}. Data references shot ({a_shotVersion.EntityRelationships.Parent.EntityId}) which is not known by the cache");
				return;
			}

			if (!m_localCache.TryGetEntityByPredicate(typeof(DataEntityPublishedFileType), a_relation => ((DataEntityPublishedFileType)a_relation).FileType == a_fileTag, out DataEntityBase? fileTag))
			{
				Logger.LogError("DataImport", $"Could not import file at path {a_sourceFilePath}. Data references file relation ({a_fileTag}) which is not known by the cache");
				return;
			}

			string targetPath = ServiceWorkerConfig.Instance.DefaultDataStoreFilePath + Path.GetFileName(a_sourceFilePath);
			ConfigStringBuilder sb = new ConfigStringBuilder();
			sb.AddReplacement("ProjectName", RemoveInvalidPathCharacters(project.Name));
			sb.AddReplacement("ShotName", RemoveInvalidPathCharacters(shot.ShotName));
			sb.AddReplacement("ShotVersionCode", RemoveInvalidPathCharacters(a_shotVersion.ShotVersionName));
			targetPath = sb.Replace(targetPath);

			lock (m_importQueue)
			{
				m_importQueue.Enqueue(new ImportQueueEntry(new FileCopyMetaData(a_sourceFilePath, targetPath, targetStorage, (DataEntityPublishedFileType)fileTag), a_shotVersion));
				m_queueAddedEvent.Set();
			}
		}

		private void DoBackgroundWork()
		{
			try
			{
				while (!m_dataImportThreadCancellationToken.IsCancellationRequested)
				{
					bool keyMessageSent = false;
					while (ServiceWorkerConfig.Instance.DefaultDataStoreFtpKeyFile == null)
					{
						if (!keyMessageSent)
						{
							Logger.LogError("Config", $"Waiting for private key file at \"{ServiceWorkerConfig.Instance.DefaultDataStoreFtpKeyFilePath}\"");
						}

						if (ServiceWorkerConfig.Instance.TryReloadPrivateKey())
						{
							break;
						}

						Thread.Sleep(1000);
					}

					ImportQueueEntry? resultToCopy;
					bool didDequeue;
					lock (m_importQueue)
					{
						didDequeue = m_importQueue.TryDequeue(out resultToCopy);
					}

					if (didDequeue && resultToCopy != null)
					{
						ECopyResult result = ECopyResult.UnknownFailure;
						try
						{
							if (m_importClient == null)
							{
								m_importClient = new SftpClient(ServiceWorkerConfig.Instance.DefaultDataStoreFtpHost, 22, ServiceWorkerConfig.Instance.DefaultDataStoreFtpUserName, ServiceWorkerConfig.Instance.DefaultDataStoreFtpKeyFile);
							}

							if (!m_importClient.IsConnected)
							{
								m_importClient.Connect();
							}

							OnCopyStarted.Invoke(resultToCopy.TargetShotVersion, resultToCopy.CopyMetaData);

							result = CopyFileWithProgress(m_importClient, resultToCopy.TargetShotVersion, resultToCopy.CopyMetaData);

							if (result == ECopyResult.Success)
							{
								OnCopyStartWriteMetaData.Invoke(resultToCopy.TargetShotVersion, resultToCopy.CopyMetaData);
								WriteMetadata(resultToCopy);
							}

							OnCopyFinished.Invoke(resultToCopy.TargetShotVersion, resultToCopy.CopyMetaData, result);
						}
						catch (SshException ex)
						{
							Logger.LogError("DataImporter", $"Failed to copy file. SshException occurred: {ex}");
						}

					}
					else
					{
						if (m_importClient != null && m_importClient.IsConnected)
						{
							m_importClient.Disconnect();
						}

						WaitHandle.WaitAny(new[] {m_dataImportThreadCancellationToken.Token.WaitHandle, m_queueAddedEvent});
					}
				}
			}
			catch (Exception ex)
			{
				Logger.LogError("DataImportWorker", $"Thread terminated with an unhandled exception: {ex.Message}");
			}
		}

		private void WriteMetadata(ImportQueueEntry a_resultToCopy)
		{
			string targetPath = Path.ChangeExtension(a_resultToCopy.CopyMetaData.SourceFilePath.LocalPath, "imported");
			using FileStream targetStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
			using TextWriter textWriter = new StreamWriter(targetStream);

			ImportedFileMetaData importedMeta = new ImportedFileMetaData(a_resultToCopy.TargetShotVersion, m_localCache);
			JsonSerializer.CreateDefault().Serialize(textWriter, importedMeta);
		}

		private ECopyResult CopyFileWithProgress(SftpClient a_client, DataEntityShotVersion a_shotVersion, FileCopyMetaData a_copyMetaData)
		{
			string ftpTargetPath = Path.Combine(ServiceWorkerConfig.Instance.DefaultDataStoreFtpRelativeRoot, a_copyMetaData.DestinationRelativeFilePath);

			string targetDirectory = ftpTargetPath.Substring(0, ftpTargetPath.LastIndexOf('/'));
			CreateRemoteDirectoryRecursively(a_client, targetDirectory);

			if (a_client.Exists(ftpTargetPath))
			{
				SftpFileAttributes targetFileInfo = a_client.GetAttributes(ftpTargetPath);
				FileInfo sourceFileInfo = new FileInfo(a_copyMetaData.SourceFilePath.LocalPath);
				if (sourceFileInfo.Length == targetFileInfo.Size)
				{
					Logger.LogInfo("DataImporter", $"Skipped file {a_copyMetaData.SourceFilePath}. Destination file seems up to date");
					return ECopyResult.FileAlreadyUpToDate;
				}

			}

			using FileStream sourceStream = new FileStream(a_copyMetaData.SourceFilePath.LocalPath, FileMode.Open, FileAccess.Read);

			long sourceSize = sourceStream.Length;

			Stopwatch sw = new Stopwatch();
			sw.Start();
			long lastBytesCopied = 0;

			a_client.UploadFile(sourceStream, ftpTargetPath, true, (a_bytesCopied) => {
				TimeSpan timeElapsed = sw.Elapsed;

				double elapsedSeconds = timeElapsed.TotalSeconds;
				if (elapsedSeconds > 0.5)
				{
					sw.Restart();

					long bytesCopied = (long) a_bytesCopied;
					long currentBlockSize = bytesCopied - lastBytesCopied;
					lastBytesCopied = bytesCopied;
					long bytesPerSecond = (long)Math.Floor(currentBlockSize / elapsedSeconds);

					float percentageCopied = ((float)lastBytesCopied / (float)sourceSize);
					OnCopyUpdate(a_shotVersion, a_copyMetaData, new FileCopyProgress(sourceSize, bytesCopied, percentageCopied, bytesPerSecond));
				}
			});

			return ECopyResult.Success;
		}

		private void CreateRemoteDirectoryRecursively(SftpClient a_client, string a_targetDirectory)
		{
			int lastIndex = 0;
			do
			{
				lastIndex = a_targetDirectory.IndexOf('/', lastIndex + 1);
				string path = a_targetDirectory.Substring(0, (lastIndex == -1)? a_targetDirectory.Length: lastIndex);
				if (!a_client.Exists(path) || !a_client.GetAttributes(path).IsDirectory)
				{
					a_client.CreateDirectory(path);
					Logger.LogInfo("DataImporter", $"Creating output directory {path}.");
				}
			} while (lastIndex != -1);
		}

		private string RemoveInvalidPathCharacters(string a_pathVariable)
		{
			string pathVar = a_pathVariable;
			foreach (var invalid in Path.GetInvalidFileNameChars())
			{
				pathVar = pathVar.Replace(invalid, '_');
			}

			return pathVar;
		}
	}
}
