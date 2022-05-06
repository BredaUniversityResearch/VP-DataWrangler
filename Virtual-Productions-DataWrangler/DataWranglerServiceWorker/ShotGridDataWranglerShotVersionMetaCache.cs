using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataWranglerCommon;
using Newtonsoft.Json;
using ShotGridIntegration;

namespace DataWranglerServiceWorker
{
	public class ShotGridDataWranglerShotVersionMetaCache
	{
		private class ShotVersionMetaCacheEntry
		{
			public ShotVersionIdentifier Identifier;
			public DataWranglerShotVersionMeta MetaValues;

			public ShotVersionMetaCacheEntry(int a_projectId, int a_shotId, int a_versionId, DataWranglerShotVersionMeta a_metaValues)
			{
				Identifier = new ShotVersionIdentifier(a_projectId, a_shotId, a_versionId);
				MetaValues = a_metaValues;
			}
		};

		private class ShotVersionIdentifier
		{
			public readonly int ProjectId;
			public readonly int ShotId;
			public readonly int VersionId;

			public ShotVersionIdentifier(int a_projectId, int a_shotId, int a_versionId)
			{
				ProjectId = a_projectId;
				ShotId = a_shotId;
				VersionId = a_versionId;
			}

			private bool Equals(ShotVersionIdentifier a_other)
			{
				return ProjectId == a_other.ProjectId && ShotId == a_other.ShotId && VersionId == a_other.VersionId;
			}

			public override bool Equals(object? a_obj)
			{
				if (ReferenceEquals(null, a_obj)) return false;
				if (ReferenceEquals(this, a_obj)) return true;
				if (a_obj.GetType() != this.GetType()) return false;
				return Equals((ShotVersionIdentifier)a_obj);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(ProjectId, ShotId, VersionId);
			}
		};

		private Dictionary<ShotVersionIdentifier, ShotVersionMetaCacheEntry> m_availableVersionMeta = new Dictionary<ShotVersionIdentifier, ShotVersionMetaCacheEntry>();

		private ShotGridAPI m_targetApi;
		private DateTimeOffset m_lastCacheUpdateTime = DateTimeOffset.MinValue;

		public ShotGridDataWranglerShotVersionMetaCache(ShotGridAPI a_targetApi)
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

				ShotGridAPIResponse<ShotGridEntityShot[]> shotsInProject = await m_targetApi.GetShotsForProject(project.Id);
				if (shotsInProject.IsError)
				{
					Logger.LogError("MetaCache", $"Failed to fetch shots for project {project.Id}: {activeProjects.ErrorInfo}");
					continue;
				}

				foreach (ShotGridEntityShot shot in shotsInProject.ResultData)
				{
					Logger.LogInfo("MetaCache", $"Fetched data for shot {shot.Id}");

					ShotGridAPIResponse<ShotGridEntityShotVersion[]> shotVersionsForShot = await m_targetApi.GetVersionsForShot(shot.Id);
					if (shotVersionsForShot.IsError)
					{
						Logger.LogError("MetaCache", $"Failed to fetch shot versions for Shot: {shot.Id} Project: {project.Id}: {activeProjects.ErrorInfo}");
						continue;
					}

					foreach (ShotGridEntityShotVersion version in shotVersionsForShot.ResultData)
					{
						if (version.Attributes.DataWranglerMeta != null)
						{
							DataWranglerShotVersionMeta? decodedMeta = JsonConvert.DeserializeObject<DataWranglerShotVersionMeta>(version.Attributes.DataWranglerMeta);
							if (decodedMeta != null)
							{
								Logger.LogInfo("MetaCache", $"Got valid meta for shot version {version.Id}");
								AddOrUpdateMeta(new ShotVersionMetaCacheEntry(project.Id, shot.Id,
									version.Id, decodedMeta));
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
	}
}
