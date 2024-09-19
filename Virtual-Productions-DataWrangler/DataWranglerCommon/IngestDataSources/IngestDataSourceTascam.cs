using Newtonsoft.Json;
using AutoNotify;
using CommonLogging;
using System.Text.RegularExpressions;
using DataApiCommon;
using DataWranglerCommon.CameraHandling;

namespace DataWranglerCommon.IngestDataSources
{
	public partial class IngestDataSourceMetaTascam: IngestDataSourceMeta
	{
		public override string SourceType => "Tascam DR-60D MkII";

		[AutoNotify, JsonProperty("FilePrefix"), IngestDataEditable(EDataEditFlags.Editable, EDataEditFlags.Editable)]
		private string m_filePrefix = "TASCAM_";

		[AutoNotify, JsonProperty("FileIndex"), IngestDataEditable(EDataEditFlags.Editable, EDataEditFlags.Editable)]
		private int m_fileIndex = 0;

		public override IngestDataSourceMeta Clone()
		{
			return new IngestDataSourceMetaTascam()
			{
				m_filePrefix = m_filePrefix,
				m_fileIndex = m_fileIndex
			};
		}
	}

	public class IngestDataSourceHandlerTascam : IngestDataSourceHandler
	{	
		private int m_nextFileIndex = 1;

		public override void InstallHooks(DataWranglerEventDelegates a_eventDelegates, DataWranglerServices a_services)
		{
			a_eventDelegates.OnRecordingStartedBeforeShotDataCreated += OnRecordingStarted;
		}

		private void OnRecordingStarted(ActiveCameraInfo a_sourceCamera, IngestDataShotVersionMeta a_shotMetaData)
		{
			IngestDataSourceMetaTascam? tascamMeta = a_shotMetaData.FindMetaByType<IngestDataSourceMetaTascam>();
			if (tascamMeta != null)
			{
				tascamMeta.FileIndex = m_nextFileIndex;
				++m_nextFileIndex;
			}
		}
	};

	public class IngestDataSourceResolverTascam : IngestDataSourceResolver
	{
		private const string FileTag = "audio";

		public IngestDataSourceResolverTascam()
			: base(true, false)
		{
		}

		public override List<IngestFileResolutionDetails> ProcessDirectory(string a_baseDirectory, string a_storageVolumeName, DataEntityCache a_cache, IngestDataCache a_ingestCache)
		{
			List<IngestFileResolutionDetails> result = new List<IngestFileResolutionDetails>();

			string sysFilePath = Path.Combine(a_baseDirectory, "dr-1.sys");
			if (!File.Exists(sysFilePath))
			{
				//TASCAM always writes a dr-1.sys file in the root. If this does not exist we can assume that this is not a relevant target.
				Logger.LogInfo("FileMetaResolverWorker", $"TASCAM meta resolver skipping {a_baseDirectory} due to absence of dr-1.sys file ({sysFilePath})");
				return result;
			}

			string fileBasePath = Path.Combine(a_baseDirectory, "MUSIC");
			if (!Directory.Exists(fileBasePath))
			{
				Logger.LogInfo("FileMetaResolverWorker", $"TASCAM meta resolver skipping {a_baseDirectory} due to absence of MUSIC folder ({fileBasePath})");
			}

			var relevantCacheEntries = a_ingestCache.FindShotVersionsWithMeta<IngestDataSourceMetaTascam>();

			foreach (string filePath in Directory.EnumerateFiles(fileBasePath))
			{
				IngestFileResolutionDetails details = new IngestFileResolutionDetails(filePath);
				result.Add(details);

				FileInfo fileInfo = new FileInfo(filePath);
				foreach (var cacheEntry in relevantCacheEntries)
				{
					IngestDataSourceMetaTascam tascamMeta = cacheEntry.Value;
					if (IsSourceFor(fileInfo, tascamMeta))
					{
						Logger.LogInfo("FileMetaResolverWorker", $"TASCAM meta resolver added {filePath} to import queue");
						details.SetSuccessfulResolution(cacheEntry.Key, FileTag);
					}
				}
			}

			return result;
		}

		public bool IsSourceFor(FileInfo a_fileInfo, IngestDataSourceMetaTascam a_meta)
		{
			//TASCAM_0040S12.wav, TASCAM_0040S34D06.wav
			Regex filePattern = new Regex($"{a_meta.FilePrefix}([0-9]{{4}})(.*).wav");
			Match fileNameMatch = filePattern.Match(a_fileInfo.Name);
			if (fileNameMatch.Success)
			{
				if (int.TryParse(fileNameMatch.Groups[1].Value, out int targetFileIndex))
				{
					if (a_meta.FileIndex == targetFileIndex)
					{
						return true;
					}
				}
			}

			return false;
		}
	};
}
