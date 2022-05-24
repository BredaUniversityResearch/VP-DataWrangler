using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using DataWranglerCommon;
using ShotGridIntegration;

namespace DataWranglerServiceWorker
{
	public class DataImportWorker
	{
		private class ImportQueueEntry
		{
			public string SourcePath;
			public string TargetPath;
			public ShotVersionIdentifier TargetShotVersion;

			public ImportQueueEntry(string a_sourcePath, string a_targetPath, ShotVersionIdentifier a_targetShotVersion)
			{
				SourcePath = a_sourcePath;
				TargetPath = a_targetPath;
				TargetShotVersion = a_targetShotVersion;
			}
		};

		private const string DefaultLocation = "//cradlenas/Projects/VirtualProductions/${ProjectName}/Shots/${ShotCode}/${ShotVersionCode}/";
		private const int DefaultCopyBufferSize = 32 * 1024 * 1024;

		public delegate void CopyStartedDelegate(string sourceFile, string destinationFile);
		public delegate void CopyProgressUpdate(string destinationFile, float progressPercent);
		public delegate void CopyFinishedDelegate(string destinationFile);

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

		public void AddFileToImport(ShotVersionIdentifier a_shotVersionIdentifier, string a_sourceFilePath)
		{
			string targetPath = DefaultLocation + Path.GetFileName(a_sourceFilePath);
			if (!m_dataCache.FindProjectForId(a_shotVersionIdentifier.ProjectId, out ShotGridEntityProject? project))
			{
				Logger.LogError("DataImport", $"Could not import file at path {a_sourceFilePath}. Data references project ({a_shotVersionIdentifier.ProjectId}) which is not known by the cache");
				return;
			}

			if (!m_dataCache.FindShotForId(a_shotVersionIdentifier.ShotId, out ShotGridEntityShot? shot))
			{
				Logger.LogError("DataImport", $"Could not import file at path {a_sourceFilePath}. Data references shot ({a_shotVersionIdentifier.ShotId}) which is not known by the cache");
				return;
			}

			if (!m_dataCache.FindShotVersionForId(a_shotVersionIdentifier.VersionId, out ShotGridEntityShotVersion? shotVersion))
			{
				Logger.LogError("DataImport", $"Could not import file at path {a_sourceFilePath}. Data references shot version ({a_shotVersionIdentifier.VersionId}) which is not known by the cache");
				return;
			}


			Dictionary<string, string> replacements = new Dictionary<string, string>
			{
				{"ProjectName", RemoveInvalidPathCharacters(project.Attributes.Name) },
				{"ShotCode", RemoveInvalidPathCharacters(shot.Attributes.ShotCode) },
				{"ShotVersionCode", RemoveInvalidPathCharacters(shotVersion.Attributes.VersionCode) }
			};

			targetPath = ResolvePath(targetPath, replacements);

			lock (m_importQueue)
			{
				m_importQueue.Enqueue(new ImportQueueEntry(a_sourceFilePath, targetPath, a_shotVersionIdentifier));
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
					OnCopyStarted.Invoke(resultToCopy.SourcePath, resultToCopy.TargetPath);
					try
					{
						CopyFileWithProgress(resultToCopy.SourcePath, resultToCopy.TargetPath);
					}
					catch (IOException ex)
					{
						Logger.LogError("DataImporter", $"Import exception occurred processing file {resultToCopy.SourcePath} => {resultToCopy.TargetPath} Exception: {ex.Message}");
					}

					OnCopyFinished.Invoke(resultToCopy.TargetPath);

					Debugger.Break(); //Link path from nas to take in the Published file section.
				}
				else
				{
					WaitHandle.WaitAny(new[] {m_dataImportThreadCancellationToken.Token.WaitHandle, m_queueAddedEvent});
				}
			}
		}

		private void CopyFileWithProgress(string a_sourcePath, string a_targetPath)
		{
			string targetDirectory = Path.GetDirectoryName(a_targetPath)!;
			if (!Directory.Exists(targetDirectory))
			{
				new DirectoryInfo(targetDirectory).Create();
			}

			FileInfo targetFileInfo = new FileInfo(a_targetPath);
			if (targetFileInfo.Exists)
			{
				FileInfo sourceFileInfo = new FileInfo(a_sourcePath);
				if (sourceFileInfo.Length <= targetFileInfo.Length)
				{
					throw new IOException("Target file already exists, and size is equal.");
				}

			}

			byte[] copyBuffer = new byte[DefaultCopyBufferSize];

			using FileStream sourceStream = new FileStream(a_sourcePath, FileMode.Open, FileAccess.Read);
			using FileStream targetStream = new FileStream(a_targetPath, FileMode.Create, FileAccess.Write);

			long sourceSize = sourceStream.Length;
			long bytesCopied = 0;

			int currentBlockSize = 0;
			while ((currentBlockSize = sourceStream.Read(copyBuffer, 0, copyBuffer.Length)) > 0)
			{
				targetStream.Write(copyBuffer, 0, currentBlockSize);
				bytesCopied += currentBlockSize;

				float percentageCopied = ((float) bytesCopied / (float) sourceSize);
				OnCopyUpdate(a_targetPath, percentageCopied);
			}
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
