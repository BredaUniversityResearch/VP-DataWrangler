using System;
using System.Threading.Tasks;
using CommonLogging;
using DataApiCommon;
using DataWranglerCommon;
using DataWranglerCommon.IngestDataSources;
using Newtonsoft.Json;
using ShotGridIntegration;

namespace DataWranglerServiceWorker
{
    public class ShotGridDataRequester
	{
		private DataApi m_targetApi;
		private DateTimeOffset m_lastCacheUpdateTime = DateTimeOffset.MinValue;

		public ShotGridDataRequester(DataApi a_targetApi)
		{
			m_targetApi = a_targetApi;
		}

		public async Task RequestAllRelevantData()
		{
			try
			{
				DataApiResponse<DataEntityLocalStorage[]> activeStores = await m_targetApi.GetLocalStorages();
				if (activeStores.IsError)
				{
					Logger.LogError("MetaCache", "Failed to fetch active stores: " + activeStores.ErrorInfo);
					return;
				}

				DataApiResponse<DataEntityPublishedFileType[]> fileTagRelations = await m_targetApi.GetPublishedFileTypes();
				if (fileTagRelations.IsError)
				{
					Logger.LogError("MetaCache", "Failed to fetch file relations: " + fileTagRelations.ErrorInfo);
					return;
				}

				DataApiResponse<DataEntityProject[]> activeProjects = await m_targetApi.GetActiveProjects();
				if (activeProjects.IsError)
				{
					Logger.LogError("MetaCache", "Failed to fetch projects: " + activeProjects.ErrorInfo);
					return;
				}

				foreach (DataEntityProject project in activeProjects.ResultData)
				{
					Logger.LogInfo("MetaCache", $"Fetched data for project {project.Name} ({project.EntityId})");

					DataApiResponse<DataEntityShot[]> shotsInProject = await m_targetApi.GetShotsForProject(project.EntityId);
					if (shotsInProject.IsError)
					{
						Logger.LogError("MetaCache", $"Failed to fetch shots for project {project.EntityId}: {activeProjects.ErrorInfo}");
						continue;
					}

					foreach (DataEntityShot shot in shotsInProject.ResultData)
					{
						Logger.LogInfo("MetaCache", $"Fetched data for shot {shot.ShotName} ({shot.EntityId})");

						DataApiResponse<DataEntityShotVersion[]> shotVersionsForShot = await m_targetApi.GetVersionsForShot(shot.EntityId);
						if (shotVersionsForShot.IsError)
						{
							Logger.LogError("MetaCache", $"Failed to fetch shot versions for Shot: {shot.ShotName} ({shot.EntityId}) Project: {project.Name} ({project.EntityId}): {activeProjects.ErrorInfo}");
							continue;
						}

						foreach (DataEntityShotVersion version in shotVersionsForShot.ResultData)
						{
							if (version.DataWranglerMeta != null)
							{
								try
								{
									IngestDataShotVersionMeta? decodedMeta = JsonConvert.DeserializeObject<IngestDataShotVersionMeta>(version.DataWranglerMeta, DataWranglerSerializationSettings.Instance);
									if (decodedMeta != null)
									{
										Logger.LogInfo("MetaCache", $"Got valid meta for shot version {version.EntityId}");
									}
								}
								catch (JsonReaderException ex)
								{
									Logger.LogError("MetaCache",
										$"Failed to read json data for shot version {project.Name}/{shot.ShotName}/{version.ShotVersionName} ({version.EntityId}). Exception: {ex.Message}");
								}
								catch (JsonSerializationException ex)
								{
									Logger.LogError("MetaCache", $"Failed to deserialize data for shot version {project.Name}/{shot.ShotName}/{version.ShotVersionName} ({version.EntityId}). Exception: {ex.Message}");
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
