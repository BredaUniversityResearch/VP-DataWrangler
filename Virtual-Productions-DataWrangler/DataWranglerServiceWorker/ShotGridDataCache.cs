using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Windows.Diagnostics;
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
		private Dictionary<ShotGridEntityName, Dictionary<int, ShotGridEntity>> m_cachedEntitiesByType = new(new ShotGridEntityName.NameTypeEqualityComparer());

		private ShotGridAPI m_targetApi;
		private DateTimeOffset m_lastCacheUpdateTime = DateTimeOffset.MinValue;

		public ShotGridDataCache(ShotGridAPI a_targetApi)
		{
			m_targetApi = a_targetApi;
		}

		public async Task UpdateCache()
		{
			try
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

				ShotGridAPIResponse<ShotGridEntityRelation[]> fileTagRelations = await m_targetApi.GetRelations(ShotGridEntityName.PublishedFileType);
				if (fileTagRelations.IsError)
				{
					Logger.LogError("MetaCache", "Failed to fetch file relations: " + fileTagRelations.ErrorInfo);
					return;
				}

				foreach (ShotGridEntityRelation fileTagRelation in fileTagRelations.ResultData)
				{
					AddOrUpdateCachedEntity(ShotGridEntityName.PublishedFileType, fileTagRelation);
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
			catch (Exception ex)
			{
				Logger.LogError("MetaCache", $"Exception occured during cache update: {ex.Message}");
				throw;
			}
		}

		private void AddOrUpdateCachedEntity<TEntityType>(TEntityType a_entry)
			where TEntityType : ShotGridEntity
		{
			AddOrUpdateCachedEntity(ShotGridEntityName.FromType<TEntityType>(), a_entry);
		}

		private void AddOrUpdateCachedEntity(ShotGridEntityName a_entityType, ShotGridEntity a_entry)
		{
			Dictionary<int, ShotGridEntity>? entitiesById = null;
			if (!m_cachedEntitiesByType.TryGetValue(a_entityType, out entitiesById))
			{
				entitiesById = new Dictionary<int, ShotGridEntity>();
				m_cachedEntitiesByType.Add(a_entityType, entitiesById);
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

		public List<KeyValuePair<TMetaType, ShotVersionMetaCacheEntry>> FindShotVersionWithMeta<TMetaType>()
			where TMetaType: DataWranglerFileSourceMeta
		{
			List<KeyValuePair<TMetaType, ShotVersionMetaCacheEntry>> result = new();
			foreach (ShotVersionMetaCacheEntry entry in m_availableVersionMeta.Values)
			{
				DataWranglerFileSourceMeta? foundRelevantMeta = entry.MetaValues.FileSources.Find((a_meta) => a_meta is TMetaType);
				if (foundRelevantMeta != null)
				{
					result.Add(new KeyValuePair<TMetaType, ShotVersionMetaCacheEntry>((TMetaType)foundRelevantMeta, entry));
				}
			}
				
			return result;
		}

		public bool FindEntityById<TEntityType>(int a_id, [NotNullWhen(true)] out TEntityType? a_result)
			where TEntityType: ShotGridEntity
		{
			a_result = null;
			if (m_cachedEntitiesByType.TryGetValue(ShotGridEntityName.FromType<TEntityType>(), out var entitiesById))
			{
				if (entitiesById.TryGetValue(a_id, out var entity))
				{
					a_result = (TEntityType) entity;
					return true;
				}
			}

			return false;
		}

		public bool FindEntity<TEntityType>(ShotGridEntityName a_entityType, Func<TEntityType, bool> a_selector, [NotNullWhen(true)] out TEntityType? a_result)
			where TEntityType: ShotGridEntity
		{
			a_result = null;
			if (m_cachedEntitiesByType.TryGetValue(a_entityType, out var entitiesById))
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

		public bool FindEntity<TEntityType>(Func<TEntityType, bool> a_selector, [NotNullWhen(true)] out TEntityType? a_result)
			where TEntityType: ShotGridEntity
		{
			return FindEntity(ShotGridEntityName.FromType<TEntityType>(), a_selector, out a_result);
		}
	}
}
