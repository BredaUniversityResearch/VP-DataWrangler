using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DataApiCommon
{
	public abstract class DataApi
	{
		public readonly DataEntityCache LocalCache = new DataEntityCache();

		public abstract Task<bool> StartConnect();

		public abstract Task<DataApiResponse<DataEntityProject[]>> GetActiveProjects();
		public abstract Task<DataApiResponse<DataEntityFilePublish>> CreateFilePublish(Guid a_projectId, Guid a_shotId, Guid a_shotVersionId, DataEntityFilePublish a_publishData);
		public abstract Task<DataApiResponse<DataEntityShot>> CreateNewShot(Guid a_projectId, DataEntityShot a_shotData);
		public abstract Task<DataApiResponse<DataEntityShotVersion>> CreateNewShotVersion(Guid a_projectId, Guid a_targetShotId, DataEntityShotVersion a_versionData);
		public abstract Task<DataApiResponse<DataEntityLocalStorage[]>> GetLocalStorages();
		public abstract Task<DataApiResponse<DataEntityShot[]>> GetShotsForProject(Guid a_projectId);

		public abstract Task<DataApiResponse<DataEntityPublishedFileType[]>> GetPublishedFileTypes();

		public abstract Task<DataApiResponse<DataEntityShotVersion[]>> GetVersionsForShot(Guid a_shotEntityId);

		public async Task<DataApiResponse<DataEntityShotVersion[]>> GetVersionsForShot(Guid a_shotEntityId, Comparison<DataEntityShotVersion> a_sortComparer)
		{
			DataApiResponse<DataEntityShotVersion[]> unsorted = await GetVersionsForShot(a_shotEntityId);
			if (!unsorted.IsError)
			{
				Array.Sort(unsorted.ResultData, a_sortComparer);
			}

			return unsorted;
		}

		public abstract Task<DataApiResponseGeneric> UpdateEntityProperties(DataEntityBase a_targetEntity, Dictionary<PropertyInfo, object?> a_changedValues);

		public async Task<DataApiResponse<TDataEntityType>> UpdateEntityProperties<TDataEntityType>(Guid a_entityId, Dictionary<PropertyInfo, object?> a_changedValues)
			where TDataEntityType: DataEntityBase
		{
			TDataEntityType? entity = LocalCache.FindEntityById<TDataEntityType>(a_entityId);
			if (entity == null)
			{
				throw new Exception($"Tried to update entity ({typeof(TDataEntityType).Name}) with id {a_entityId} which was not known by the cache.");
			}

			DataApiResponseGeneric response = await UpdateEntityProperties(entity, a_changedValues);
			return new DataApiResponse<TDataEntityType>(response);
		}

		protected void OnDataEntitySuccessfullyFetched(DataEntityBase a_entity)
		{
			LocalCache.AddCachedEntity(a_entity.GetType(), a_entity);
		}
	}

};
