using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using CommonLogging;
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
			public ShotVersionIdentifier TargetShotVersion;

			public ImportQueueEntry(FileCopyMetaData a_copyMetaData, ShotVersionIdentifier a_targetShotVersion)
			{
				CopyMetaData = a_copyMetaData;
				TargetShotVersion = a_targetShotVersion;
			}
		};

		public delegate void CopyStartedDelegate(ShotVersionIdentifier shotVersion, FileCopyMetaData metaData);
		public delegate void CopyProgressUpdate(ShotVersionIdentifier shotVersion, FileCopyMetaData metaData, FileCopyProgress progress);
		public delegate void CopyFinishedDelegate(ShotVersionIdentifier shotVersion, FileCopyMetaData metaData, ECopyResult result);

		public event CopyStartedDelegate OnCopyStarted = delegate { };
		public event CopyProgressUpdate OnCopyUpdate = delegate { };
		public event CopyFinishedDelegate OnCopyFinished = delegate { };

		private ShotGridDataCache m_dataCache;
		private ShotGridAPI m_api;
		private Queue<ImportQueueEntry> m_importQueue = new();
		public int ImportQueueLength => m_importQueue.Count;
		private AutoResetEvent m_queueAddedEvent = new AutoResetEvent(false);
		private Thread m_dataImportThread;
		private CancellationTokenSource m_dataImportThreadCancellationToken = new CancellationTokenSource();

		public DataImportWorker(ShotGridDataCache a_cache, ShotGridAPI a_api)
		{
			m_dataCache = a_cache;
			m_api = a_api;
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

		public void AddFileToImport(ShotVersionIdentifier a_shotVersionIdentifier, string a_sourceFilePath, string a_fileTag)
		{
			if (!m_dataCache.FindEntity(a_obj => a_obj.Attributes.LocalStorageName == ServiceWorkerConfig.Instance.DefaultDataStorageName, out ShotGridEntityLocalStorage? targetStorage) ||
			    string.IsNullOrEmpty(targetStorage.Attributes.WindowsPath))
			{
				Logger.LogError("DataImport", $"Could not import file at path {a_sourceFilePath}. Could not find data storage with name \"{ServiceWorkerConfig.Instance.DefaultDataStorageName}\". Importer is NOT active");
				return;
			}

			if (!m_dataCache.FindEntityById(a_shotVersionIdentifier.ProjectId, out ShotGridEntityProject? project))
			{
				Logger.LogError("DataImport", $"Could not import file at path {a_sourceFilePath}. Data references project ({a_shotVersionIdentifier.ProjectId}) which is not known by the cache");
				return;
			}

			if (!m_dataCache.FindEntityById(a_shotVersionIdentifier.ShotId, out ShotGridEntityShot? shot))
			{
				Logger.LogError("DataImport", $"Could not import file at path {a_sourceFilePath}. Data references shot ({a_shotVersionIdentifier.ShotId}) which is not known by the cache");
				return;
			}

			if (!m_dataCache.FindEntityById(a_shotVersionIdentifier.VersionId, out ShotGridEntityShotVersion? shotVersion))
			{
				Logger.LogError("DataImport", $"Could not import file at path {a_sourceFilePath}. Data references shot version ({a_shotVersionIdentifier.VersionId}) which is not known by the cache");
				return;
			}

			if (!m_dataCache.FindEntity(ShotGridEntityName.PublishedFileType, a_relation => a_relation.Attributes.Code == a_fileTag, out ShotGridEntityRelation? fileTag))
			{
				Logger.LogError("DataImport", $"Could not import file at path {a_sourceFilePath}. Data references file relation ({a_fileTag}) which is not known by the cache");
				return;
			}


			Dictionary<string, string> replacements = new Dictionary<string, string>
			{
				{"ProjectName", RemoveInvalidPathCharacters(project.Attributes.Name) },
				{"ShotCode", RemoveInvalidPathCharacters(shot.Attributes.ShotCode) },
				{"ShotVersionCode", RemoveInvalidPathCharacters(shotVersion.Attributes.VersionCode) }
			};

			string targetPath = ServiceWorkerConfig.Instance.DefaultDataStoreFilePath + Path.GetFileName(a_sourceFilePath);
			targetPath = ResolvePath(targetPath, replacements);

			lock (m_importQueue)
			{
				m_importQueue.Enqueue(new ImportQueueEntry(new FileCopyMetaData(a_sourceFilePath, targetPath, targetStorage, fileTag), a_shotVersionIdentifier));
				m_queueAddedEvent.Set();
			}
		}

		private void DoBackgroundWork()
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
						using (SftpClient ftpClient = new SftpClient(ServiceWorkerConfig.Instance.DefaultDataStoreFtpHost, 22, ServiceWorkerConfig.Instance.DefaultDataStoreFtpUserName, ServiceWorkerConfig.Instance.DefaultDataStoreFtpKeyFile))
						{
							OnCopyStarted.Invoke(resultToCopy.TargetShotVersion, resultToCopy.CopyMetaData);

							ftpClient.Connect();
							if (ftpClient.IsConnected)
							{
								result = CopyFileWithProgress(ftpClient, resultToCopy.TargetShotVersion, resultToCopy.CopyMetaData);
							}

							ftpClient.Disconnect();
						}

						if (result == ECopyResult.Success)
						{
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
					WaitHandle.WaitAny(new[] {m_dataImportThreadCancellationToken.Token.WaitHandle, m_queueAddedEvent});
				}
			}
		}

		private void WriteMetadata(ImportQueueEntry a_resultToCopy)
		{
			string targetPath = Path.ChangeExtension(a_resultToCopy.CopyMetaData.SourceFilePath.LocalPath, "imported");
			using FileStream targetStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
			using TextWriter textWriter = new StreamWriter(targetStream);

			ImportedFileMetaData importedMeta = new ImportedFileMetaData(a_resultToCopy.TargetShotVersion, m_dataCache);
			JsonSerializer.CreateDefault().Serialize(textWriter, importedMeta);
		}

		private ECopyResult CopyFileWithProgress(SftpClient a_client, ShotVersionIdentifier a_shotVersion, FileCopyMetaData a_copyMetaData)
		{
			string ftpTargetPath = Path.Combine(ServiceWorkerConfig.Instance.DefaultDataStoreFtpRelativeRoot, a_copyMetaData.DestinationRelativeFilePath);

			string targetDirectory = ftpTargetPath.Substring(0, ftpTargetPath.LastIndexOf('/'));
			if (!a_client.Exists(targetDirectory))
			{
				a_client.CreateDirectory(targetDirectory);
			}

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

		private string ResolvePath(string a_inputPath, Dictionary<string, string> a_replacementVariables)
		{
			Regex regex = new Regex("\\$\\{([a-zA-Z0-9]+)\\}", RegexOptions.CultureInvariant);
			MatchCollection matches = regex.Matches(a_inputPath);

			string output = a_inputPath;
			foreach (Match match in matches)
			{
				string targetValue = match.Groups[1].Value;
				if (a_replacementVariables.TryGetValue(targetValue, out string? replacement))
				{
					output = output.Replace(match.Value, replacement);
				}
			}

			return output;
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
