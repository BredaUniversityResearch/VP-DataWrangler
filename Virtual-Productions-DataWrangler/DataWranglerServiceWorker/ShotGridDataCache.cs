using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using DataWranglerCommon;
using Newtonsoft.Json;
using ShotGridIntegration;

namespace DataWranglerServiceWorker
{
	public class ShotGridDataCache
	{
		public static readonly TimeSpan MaxTimeOffset = new(0, 0, 2);

		public class ShotVersionMetaCacheEntry
		{
			public ShotVersionIdentifier Identifier;
			public string ShotCode;
			public DataWranglerShotVersionMeta MetaValues;

			public ShotVersionMetaCacheEntry(int a_projectId, int a_shotId, int a_versionId, string a_shotCode, DataWranglerShotVersionMeta a_metaValues)
			{
				Identifier = new ShotVersionIdentifier(a_projectId, a_shotId, a_versionId);
				ShotCode = a_shotCode;
				MetaValues = a_metaValues;
			}
		};

		private Dictionary<ShotVersionIdentifier, ShotVersionMetaCacheEntry> m_availableVersionMeta = new Dictionary<ShotVersionIdentifier, ShotVersionMetaCacheEntry>();
		private Dictionary<int, ShotGridEntityProject> m_projects = new();
		private Dictionary<int, ShotGridEntityShot> m_shots = new();
		private Dictionary<int, ShotGridEntityShotVersion> m_shotVersions = new();

		private ShotGridAPI m_targetApi;
		private DateTimeOffset m_lastCacheUpdateTime = DateTimeOffset.MinValue;

		public ShotGridDataCache(ShotGridAPI a_targetApi)
		{
			m_targetApi = a_targetApi;
		}

		public async Task UpdateCache()
		{
			ShotGridAPIResponse<ShotGridEntityProject[]> activeProjects = await m_targetApi.GetActiveProjects();
			if (activeProjects.IsError)
			{
				Logger.LogError("MetaCache", "Failed to fetch projects: " + activeProjects.ErrorInfo);
				return;
			}

			foreach (ShotGridEntityProject project in activeProjects.ResultData)
			{
				Logger.LogInfo("MetaCache", $"Fetched data for project {project.Id}");

				m_projects.Add(project.Id, project);

				ShotGridAPIResponse<ShotGridEntityShot[]> shotsInProject = await m_targetApi.GetShotsForProject(project.Id);
				if (shotsInProject.IsError)
				{
					Logger.LogError("MetaCache", $"Failed to fetch shots for project {project.Id}: {activeProjects.ErrorInfo}");
					continue;
				}

				foreach (ShotGridEntityShot shot in shotsInProject.ResultData)
				{
					Logger.LogInfo("MetaCache", $"Fetched data for shot {shot.Id}");

					m_shots.Add(shot.Id, shot);

					ShotGridAPIResponse<ShotGridEntityShotVersion[]> shotVersionsForShot = await m_targetApi.GetVersionsForShot(shot.Id);
					if (shotVersionsForShot.IsError)
					{
						Logger.LogError("MetaCache", $"Failed to fetch shot versions for Shot: {shot.Id} Project: {project.Id}: {activeProjects.ErrorInfo}");
						continue;
					}

					foreach (ShotGridEntityShotVersion version in shotVersionsForShot.ResultData)
					{
						m_shotVersions.Add(version.Id, version);

						if (version.Attributes.DataWranglerMeta != null)
						{
							DataWranglerShotVersionMeta? decodedMeta = JsonConvert.DeserializeObject<DataWranglerShotVersionMeta>(version.Attributes.DataWranglerMeta);
							if (decodedMeta != null)
							{
								Logger.LogInfo("MetaCache", $"Got valid meta for shot version {version.Id}");
								AddOrUpdateMeta(new ShotVersionMetaCacheEntry(project.Id, shot.Id,
									version.Id, version.Attributes.VersionCode, decodedMeta));
							}
						}
					}
				}
			}

			m_lastCacheUpdateTime = DateTimeOffset.UtcNow;
		}

		private void AddOrUpdateMeta(ShotVersionMetaCacheEntry a_cacheEntry)
		{
			if (m_availableVersionMeta.TryGetValue(a_cacheEntry.Identifier, out var existingCacheEntry))
			{
				existingCacheEntry.MetaValues = a_cacheEntry.MetaValues;
			}
			else
			{
				m_availableVersionMeta.Add(a_cacheEntry.Identifier, a_cacheEntry);
			}
		}

		public bool FindProjectForId(int a_projectId, [NotNullWhen(true)] out ShotGridEntityProject? a_projectData)
		{
			return m_projects.TryGetValue(a_projectId, out a_projectData);
		}

		public bool FindShotForId(int a_shotId, [NotNullWhen(true)] out ShotGridEntityShot? a_shotData)
		{
			return m_shots.TryGetValue(a_shotId, out a_shotData);
		}

		public bool FindShotVersionForId(int a_shotId, [NotNullWhen(true)] out ShotGridEntityShotVersion? a_shotData)
		{
			return m_shotVersions.TryGetValue(a_shotId, out a_shotData);
		}

		public bool FindShotVersionForFile(DateTimeOffset a_fileInfoCreationTimeUtc, string a_storageName, ECameraCodec a_codec, [NotNullWhen(true)] out ShotVersionMetaCacheEntry? a_shotVersionMetaCacheEntry)
		{
			foreach (ShotVersionMetaCacheEntry entry in m_availableVersionMeta.Values)
			{
				if (entry.MetaValues.Video.CodecName == a_codec.ToString() &&
				    entry.MetaValues.Video.StorageTarget == a_storageName)
				{
					TimeSpan? timeSinceCreation = a_fileInfoCreationTimeUtc - entry.MetaValues.Video.RecordingStart!;
					if (timeSinceCreation > -MaxTimeOffset && timeSinceCreation < MaxTimeOffset)
					{
						a_shotVersionMetaCacheEntry = entry;
						return true;
					}
				}
			}

			a_shotVersionMetaCacheEntry = null;
			return false;
		}
	}
}
