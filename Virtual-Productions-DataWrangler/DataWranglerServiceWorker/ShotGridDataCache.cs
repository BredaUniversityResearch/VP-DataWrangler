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

		private Dictionary<ShotVersionIdentifier, ShotVersionMetaCacheEntry> m_availableVersionMeta = new();
		private Dictionary<string, Dictionary<int, ShotGridEntity>> m_cachedEntitiesByType = new();

		private ShotGridAPI m_targetApi;
		private DateTimeOffset m_lastCacheUpdateTime = DateTimeOffset.MinValue;

		public ShotGridDataCache(ShotGridAPI a_targetApi)
		{
			m_targetApi = a_targetApi;
		}

		public async Task UpdateCache()
		{
			ShotGridAPIResponse<ShotGridEntityLocalStorage[]> activeStores = await m_targetApi.GetLocalStorages();
			if (activeStores.IsError)
			{
				Logger.LogError("MetaCache", "Failed to fetch active stores: " + activeStores.ErrorInfo);
				return;
			}

			foreach (ShotGridEntityLocalStorage localStore in activeStores.ResultData)
			{
				AddOrUpdateCachedEntity(localStore);
			}

			ShotGridAPIResponse<ShotGridEntityProject[]> activeProjects = await m_targetApi.GetActiveProjects();
			if (activeProjects.IsError)
			{
				Logger.LogError("MetaCache", "Failed to fetch projects: " + activeProjects.ErrorInfo);
				return;
			}

			foreach (ShotGridEntityProject project in activeProjects.ResultData)
			{
				Logger.LogInfo("MetaCache", $"Fetched data for project {project.Id}");

				AddOrUpdateCachedEntity(project);

				ShotGridAPIResponse<ShotGridEntityShot[]> shotsInProject = await m_targetApi.GetShotsForProject(project.Id);
				if (shotsInProject.IsError)
				{
					Logger.LogError("MetaCache", $"Failed to fetch shots for project {project.Id}: {activeProjects.ErrorInfo}");
					continue;
				}

				foreach (ShotGridEntityShot shot in shotsInProject.ResultData)
				{
					Logger.LogInfo("MetaCache", $"Fetched data for shot {shot.Id}");

					AddOrUpdateCachedEntity(shot);

					ShotGridAPIResponse<ShotGridEntityShotVersion[]> shotVersionsForShot = await m_targetApi.GetVersionsForShot(shot.Id);
					if (shotVersionsForShot.IsError)
					{
						Logger.LogError("MetaCache", $"Failed to fetch shot versions for Shot: {shot.Id} Project: {project.Id}: {activeProjects.ErrorInfo}");
						continue;
					}

					foreach (ShotGridEntityShotVersion version in shotVersionsForShot.ResultData)
					{
						AddOrUpdateCachedEntity(version);

						if (version.Attributes.DataWranglerMeta != null)
						{
							try
							{
								DataWranglerShotVersionMeta? decodedMeta = JsonConvert.DeserializeObject<DataWranglerShotVersionMeta>(version.Attributes.DataWranglerMeta, DataWranglerSerializationSettings.Instance);
								if (decodedMeta != null)
								{
									Logger.LogInfo("MetaCache", $"Got valid meta for shot version {version.Id}");
									AddOrUpdateMeta(new ShotVersionMetaCacheEntry(project.Id, shot.Id,
										version.Id, version.Attributes.VersionCode, decodedMeta));
								}
							}
							catch (JsonSerializationException ex)
							{
								Logger.LogError("MetaCache", $"Failed to deserialize data for shot version {version.Id}. Exception: {ex.Message}");
							}
						}
					}
				}
			}


			m_lastCacheUpdateTime = DateTimeOffset.UtcNow;
			Logger.LogInfo("MetaCache", $"Cache updated. Last update time {DateTime.Now}");
		}

		private void AddOrUpdateCachedEntity<TEntityType>(TEntityType a_entry)
			where TEntityType : ShotGridEntity
		{
			Dictionary<int, ShotGridEntity>? entitiesById = null;
			if (!m_cachedEntitiesByType.TryGetValue(ShotGridEntity.GetEntityName<TEntityType>(), out entitiesById))
			{
				entitiesById = new Dictionary<int, ShotGridEntity>();
				m_cachedEntitiesByType.Add(ShotGridEntity.GetEntityName<TEntityType>(), entitiesById);
			}

			entitiesById[a_entry.Id] = a_entry;
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

		public bool FindShotVersionForFile(DateTimeOffset a_fileInfoCreationTimeUtc, string a_storageName, ECameraCodec a_codec, [NotNullWhen(true)] out ShotVersionMetaCacheEntry? a_shotVersionMetaCacheEntry)
		{
			foreach (ShotVersionMetaCacheEntry entry in m_availableVersionMeta.Values)
			{
				DataWranglerFileSourceMeta? meta = entry.MetaValues.HasFileSourceForFile(a_fileInfoCreationTimeUtc, a_storageName, a_codec.ToString());
				if (meta != null)
				{
					a_shotVersionMetaCacheEntry = entry;
					return true;
				}
			}

			a_shotVersionMetaCacheEntry = null;
			return false;
		}

		public bool FindEntityById<TEntityType>(int a_id, [NotNullWhen(true)] out TEntityType? a_result)
			where TEntityType: ShotGridEntity
		{
			a_result = null;
			if (m_cachedEntitiesByType.TryGetValue(ShotGridEntity.GetEntityName<TEntityType>(), out var entitiesById))
			{
				if (entitiesById.TryGetValue(a_id, out var entity))
				{
					a_result = (TEntityType) entity;
					return true;
				}
			}

			return false;
		}

		public bool FindEntity<TEntityType>(Func<TEntityType, bool> a_selector, [NotNullWhen(true)] out TEntityType? a_result)
			where TEntityType: ShotGridEntity
		{
			a_result = null;
			if (m_cachedEntitiesByType.TryGetValue(ShotGridEntity.GetEntityName<TEntityType>(), out var entitiesById))
			{
				foreach (var shotGridEntity in entitiesById.Values)
				{
					var entity = (TEntityType)shotGridEntity;
					if (a_selector.Invoke(entity))
					{
						a_result = entity;
						return true;
					}
				}
			}
			return false;
		}
	}
}
