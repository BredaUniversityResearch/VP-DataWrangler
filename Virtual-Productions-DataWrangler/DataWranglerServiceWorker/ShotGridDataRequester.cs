using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using CommonLogging;
using DataWranglerCommon;
using DataWranglerCommon.IngestDataSources;
using Newtonsoft.Json;
using ShotGridIntegration;

namespace DataWranglerServiceWorker
{
    public class ShotGridDataRequester
	{
		private ShotGridAPI m_targetApi;
		private DateTimeOffset m_lastCacheUpdateTime = DateTimeOffset.MinValue;

		public ShotGridDataRequester(ShotGridAPI a_targetApi)
		{
			m_targetApi = a_targetApi;
		}

		public async Task RequestAllRelevantData()
		{
			try
			{
				ShotGridAPIResponse<ShotGridEntityLocalStorage[]> activeStores = await m_targetApi.GetLocalStorages();
				if (activeStores.IsError)
				{
					Logger.LogError("MetaCache", "Failed to fetch active stores: " + activeStores.ErrorInfo);
					return;
				}

				ShotGridAPIResponse<ShotGridEntityRelation[]> fileTagRelations = await m_targetApi.GetRelations(ShotGridEntityName.PublishedFileType);
				if (fileTagRelations.IsError)
				{
					Logger.LogError("MetaCache", "Failed to fetch file relations: " + fileTagRelations.ErrorInfo);
					return;
				}

				ShotGridAPIResponse<ShotGridEntityProject[]> activeProjects = await m_targetApi.GetActiveProjects();
				if (activeProjects.IsError)
				{
					Logger.LogError("MetaCache", "Failed to fetch projects: " + activeProjects.ErrorInfo);
					return;
				}

				foreach (ShotGridEntityProject project in activeProjects.ResultData)
				{
					Logger.LogInfo("MetaCache", $"Fetched data for project {project.Attributes.Name} ({project.Id})");

					ShotGridAPIResponse<ShotGridEntityShot[]> shotsInProject = await m_targetApi.GetShotsForProject(project.Id);
					if (shotsInProject.IsError)
					{
						Logger.LogError("MetaCache", $"Failed to fetch shots for project {project.Id}: {activeProjects.ErrorInfo}");
						continue;
					}

					foreach (ShotGridEntityShot shot in shotsInProject.ResultData)
					{
						Logger.LogInfo("MetaCache", $"Fetched data for shot {shot.Attributes.ShotCode} ({shot.Id})");

						ShotGridAPIResponse<ShotGridEntityShotVersion[]> shotVersionsForShot = await m_targetApi.GetVersionsForShot(shot.Id);
						if (shotVersionsForShot.IsError)
						{
							Logger.LogError("MetaCache", $"Failed to fetch shot versions for Shot: {shot.Attributes.ShotCode} ({shot.Id}) Project: {project.Attributes.Name} ({project.Id}): {activeProjects.ErrorInfo}");
							continue;
						}

						foreach (ShotGridEntityShotVersion version in shotVersionsForShot.ResultData)
						{
							if (version.Attributes.DataWranglerMeta != null)
							{
								try
								{
									IngestDataShotVersionMeta? decodedMeta = JsonConvert.DeserializeObject<IngestDataShotVersionMeta>(version.Attributes.DataWranglerMeta, DataWranglerSerializationSettings.Instance);
									if (decodedMeta != null)
									{
										Logger.LogInfo("MetaCache", $"Got valid meta for shot version {version.Id}");
									}
								}
								catch (JsonReaderException ex)
								{
									Logger.LogError("MetaCache",
										$"Failed to read json data for shot version {project.Attributes.Name}/{shot.Attributes.ShotCode}/{version.Attributes.VersionCode} ({version.Id}). Exception: {ex.Message}");
								}
								catch (JsonSerializationException ex)
								{
									Logger.LogError("MetaCache", $"Failed to deserialize data for shot version {project.Attributes.Name}/{shot.Attributes.ShotCode}/{version.Attributes.VersionCode} ({version.Id}). Exception: {ex.Message}");
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
				Logger.LogError("MetaCache", $"Exception occurred during cache update: {ex.Message}");
				throw;
			}
		}
	}
}
