using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using CommonLogging;
using DataWranglerCommon;
using Newtonsoft.Json;
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

		private const string DefaultDataStorageName = "CradleNas";
		//private const string DefaultLocation = "D:/Projects/VirtualProductions/TestImportRoot/${ProjectName}/Shots/${ShotCode}/${ShotVersionCode}/";
		private const string DefaultDataStoreFilePath = "${ProjectName}/Shots/${ShotCode}/${ShotVersionCode}/"; //Relative to DataStoreRoot
		private const int DefaultCopyBufferSize = 32 * 1024 * 1024;

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
			
			if (!m_dataCache.FindEntity(a_obj => a_obj.Attributes.LocalStorageName == DefaultDataStorageName, out ShotGridEntityLocalStorage? targetStorage) ||
			    string.IsNullOrEmpty(targetStorage.Attributes.WindowsPath))
			{
				Logger.LogError("DataImport", $"Could not import file at path {a_sourceFilePath}. Could not find data storage with name \"{DefaultDataStorageName}\". Importer is NOT active");
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

			string targetPath = DefaultDataStoreFilePath + Path.GetFileName(a_sourceFilePath);
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
				ImportQueueEntry? resultToCopy;
				bool didDequeue;
				lock (m_importQueue)
				{
					didDequeue = m_importQueue.TryDequeue(out resultToCopy);
				}

				if (didDequeue && resultToCopy != null)
				{
					ECopyResult result = ECopyResult.UnknownFailure;
					OnCopyStarted.Invoke(resultToCopy.TargetShotVersion, resultToCopy.CopyMetaData);
					try
					{
						result = CopyFileWithProgress(resultToCopy.TargetShotVersion, resultToCopy.CopyMetaData);
					}
					catch (IOException ex)
					{
						Logger.LogError("DataImporter", $"Import exception occurred processing file {resultToCopy.CopyMetaData.SourceFilePath} => {resultToCopy.CopyMetaData.DestinationFullFilePath} Exception: {ex.Message}");
					}

					if (result == ECopyResult.Success)
					{
						WriteMetadata(resultToCopy);
					}

					OnCopyFinished.Invoke(resultToCopy.TargetShotVersion, resultToCopy.CopyMetaData, result);
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

		private ECopyResult CopyFileWithProgress(ShotVersionIdentifier a_shotVersion, FileCopyMetaData a_copyMetaData)
		{
			string? targetDirectory = Path.GetDirectoryName(a_copyMetaData.DestinationFullFilePath.LocalPath);
			if (targetDirectory == null)
			{
				Logger.LogError("DataImporter", $"Failed to get directory from path {a_copyMetaData.DestinationFullFilePath}");
				return ECopyResult.InvalidDestinationPath;
			}

			if (!Directory.Exists(targetDirectory))
			{
				new DirectoryInfo(targetDirectory).Create();
			}

			FileInfo targetFileInfo = new FileInfo(a_copyMetaData.DestinationFullFilePath.LocalPath);
			if (targetFileInfo.Exists)
			{
				FileInfo sourceFileInfo = new FileInfo(a_copyMetaData.SourceFilePath.LocalPath);
				if (sourceFileInfo.Length <= targetFileInfo.Length)
				{
					Logger.LogInfo("DataImporter", $"Skipped file {a_copyMetaData.SourceFilePath}. Destination file seems up to date");
					return ECopyResult.FileAlreadyUpToDate;
				}

			}

			byte[] copyBuffer = new byte[DefaultCopyBufferSize];

			using FileStream sourceStream = new FileStream(a_copyMetaData.SourceFilePath.LocalPath, FileMode.Open, FileAccess.Read);
			using FileStream targetStream = new FileStream(a_copyMetaData.DestinationFullFilePath.LocalPath, FileMode.Create, FileAccess.Write);

			long sourceSize = sourceStream.Length;
			long bytesCopied = 0;

			int currentBlockSize = 0;
			Stopwatch sw = new Stopwatch();
			sw.Start();
			while ((currentBlockSize = sourceStream.Read(copyBuffer, 0, copyBuffer.Length)) > 0)
			{
				targetStream.Write(copyBuffer, 0, currentBlockSize);
				bytesCopied += currentBlockSize;
				TimeSpan timeElapsed = sw.Elapsed;
				sw.Restart();

				double elapsedSeconds = timeElapsed.TotalSeconds;
				if (elapsedSeconds <= 0.0)
				{
					elapsedSeconds = 1.0;
				}

				long bytesPerSecond = (long)Math.Floor(currentBlockSize / elapsedSeconds);

				float percentageCopied = ((float) bytesCopied / (float) sourceSize);
				OnCopyUpdate(a_shotVersion, a_copyMetaData, new FileCopyProgress(sourceSize, bytesCopied, percentageCopied, bytesPerSecond));
			}

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
