using System.Reflection;
using DataApiCommon;

namespace DataApiFileSystem
{
	public class DataApiFileSystem: DataApi
	{
		public override Task<DataApiResponse<DataEntityProject[]>> GetActiveProjects()
		{
			throw new NotImplementedException();
		}

		public override Task<DataApiResponse<DataEntityFilePublish>> CreateFilePublish(int a_projectId, int a_parentId, int a_shotVersionId, DataEntityFilePublish a_publishData)
		{
			throw new NotImplementedException();
		}

		public override Task<DataApiResponse<DataEntityShot>> CreateNewShot(int a_projectId, DataEntityShot a_gridEntityShotAttributes)
		{
			throw new NotImplementedException();
		}

		public override Task<DataApiResponse<DataEntityShotVersion>> CreateNewShotVersion(int a_projectSelectorSelectedProjectId, int a_targetShotId, DataEntityShotVersion a_versionData)
		{
			throw new NotImplementedException();
		}

		public override Task<DataApiResponse<DataEntityLocalStorage[]>> GetLocalStorages()
		{
			throw new NotImplementedException();
		}

		public override Task<DataApiResponse<DataEntityShot[]>> GetShotsForProject(int a_projectId)
		{
			throw new NotImplementedException();
		}

		public override Task<DataApiResponse<DataEntityPublishedFileType[]>> GetPublishedFileTypes()
		{
			throw new NotImplementedException();
		}

		public override Task<DataApiResponse<DataEntityShotVersion[]>> GetVersionsForShot(int a_shotEntityId)
		{
			throw new NotImplementedException();
		}

		public override Task<DataApiResponseGeneric> UpdateEntityProperties(DataEntityBase a_targetEntity, Dictionary<PropertyInfo, object?> a_changedValues)
		{
			throw new NotImplementedException();
		}
	}
}