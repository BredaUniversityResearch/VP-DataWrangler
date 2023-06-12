using System;
using System.Collections.Generic;
using System.Reflection;
using DataApiCommon;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShotGridIntegration;

namespace ShotGridIntegrationTest
{
	[TestClass]
	public class ShotGridApiTests
	{
		private static ShotGridAPI m_api = new ShotGridAPI();

		[ClassInitialize]
		public static void SetupAuthorization(TestContext _)
		{
			ShotGridLoginResponse response = m_api.TryLogin(CredentialProvider.Username!, CredentialProvider.Password!).Result;
			Assert.IsTrue(response.Success);
		}

		[TestMethod]
		public void Login()
		{
			ShotGridLoginResponse response = m_api.TryRefreshToken().Result;
			Assert.IsTrue(response.Success);
		}

		//2022-12-09 PdG: Disabled test due to not having the credentials at the ready here.
		//[TestMethod]
		//public void OAuthLogin()
		//{
		//	ShotGridLoginResponse response = m_api.TryLoginWithScriptKey("", "").Result;
		//	Assert.IsTrue(response.Success);
		//}

		[TestMethod]
		public void GetProjects()
		{
			DataApiResponse<DataEntityProject[]> projects = m_api.GetActiveProjects().Result;

			Assert.IsFalse(projects.IsError);

			foreach (DataEntityProject project in projects.ResultData!)
			{
				Assert.IsTrue(project.Name != null);
			}
		}

		[TestMethod]
		public void GetShots()
		{
			DataApiResponse<DataEntityShot[]> shots = m_api.GetShotsForProject(TestConstants.TargetProjectId).Result;

			Assert.IsFalse(shots.IsError);
			Assert.IsTrue(shots.ResultData!.Length > 0);

			foreach (DataEntityShot shot in shots.ResultData!)
			{
				Assert.IsTrue(!string.IsNullOrEmpty(shot.ShotName));
			}
		}

		[TestMethod]
		public void GetShotsAndTestCache()
		{
			DataApiResponse<DataEntityShot[]> shots = m_api.GetShotsForProject(TestConstants.TargetProjectId).Result;

			Assert.IsFalse(shots.IsError);
			Assert.IsTrue(shots.ResultData!.Length > 0);

			foreach (DataEntityShot shot in shots.ResultData!)
			{
				Assert.IsTrue(!string.IsNullOrEmpty(shot.ShotName));
			}

			DataEntityShot[] cachedShots = m_api.LocalCache.FindEntities<DataEntityShot>(DataEntityCacheSearchFilter.ForProject(TestConstants.TargetProjectId));
			Assert.IsTrue(cachedShots.Length == shots.ResultData.Length);
			DataEntityShot[] mismatchFilterShots = m_api.LocalCache.FindEntities<DataEntityShot>(DataEntityCacheSearchFilter.ForProject(new Guid(-1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)));
			Assert.IsTrue(mismatchFilterShots.Length == 0);
		}

		[TestMethod]
		public void GetVersionsForShot()
		{
			DataApiResponse<DataEntityShotVersion[]> shotVersions = m_api.GetVersionsForShot(TestConstants.TargetShotId).Result;

			Assert.IsFalse(shotVersions.IsError);

			foreach (DataEntityShotVersion shotVersion in shotVersions.ResultData!)
			{
				Assert.IsTrue(!string.IsNullOrEmpty(shotVersion.ShotVersionName));
			}
		}

		[TestMethod]
		public void GetSortedVersionsForShot()
		{
			DataApiResponse<DataEntityShotVersion[]> shotVersions = m_api.GetVersionsForShot(TestConstants.TargetShotId).Result;

			Assert.IsFalse(shotVersions.IsError);

			foreach (DataEntityShotVersion shotVersion in shotVersions.ResultData!)
			{
				Assert.IsTrue(!string.IsNullOrEmpty(shotVersion.ShotVersionName));
			}
		}

		//PdG 2023-05-25: Disabled since this is not a unit test per-se but something that we use to query the API when we need.
		//[TestMethod]
		//public void GetPublishesSchema()
		//{
		//	DataApiResponse<ShotGridEntityFieldSchema> schemas = m_api.GetEntityFieldSchema(ShotGridEntityTypeInfo.Shot.CamelCase, TestConstants.TargetProjectId).Result;
		//	Assert.IsFalse(schemas.IsError);
		//}

		[TestMethod]
		public void GetPublishesForShot()
		{
			DataApiResponse<DataEntityFilePublish[]> shots = m_api.GetPublishesForShotVersion(ShotGridIdUtility.ToShotGridId(TestConstants.TargetShotVersionId)).Result;

			Assert.IsFalse(shots.IsError);
		}

		[TestMethod]
		public void UpdateEntityData()
		{
			DataApiResponse<DataEntityShotVersion[]> shotVersions = m_api.GetVersionsForShot(TestConstants.TargetShotId).Result;
			Assert.IsFalse(shotVersions.IsError);

			PropertyInfo? targetField = typeof(DataEntityShotVersion).GetProperty(nameof(DataEntityShotVersion.DataWranglerMeta), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
			if (targetField == null)
			{
				throw new Exception("Could not find field by name to test update with.");
			}

			DataApiResponse<DataEntityShotVersion> response = m_api.UpdateEntityProperties<DataEntityShotVersion>(
				TestConstants.TargetShotVersionId, new Dictionary<PropertyInfo, object?> {{targetField, "test"}}).Result;

			Assert.IsFalse(response.IsError);
		}

		[TestMethod]
		public void CreateFilePublish()
		{
			DataApiResponse<DataEntityPublishedFileType> fileTypeRelation = m_api.FindPublishedFileTypeByCode("video").Result;
			Assert.IsTrue(fileTypeRelation.ResultData != null);

			string targetPath = "\\\\cradlenas\\Virtual Productions\\Phils VP Pipeline Testing Playground\\Shots\\Shot 01\\Take 08\\A014_05111248_C002.braw";

			DataEntityFilePublish attributes = new DataEntityFilePublish();
			attributes.Path = new DataEntityFileLink(new Uri(targetPath));
			attributes.PublishedFileName = "Testing braw File";
			attributes.PublishedFileType = new DataEntityReference(fileTypeRelation.ResultData!);
			attributes.ShotVersion = new DataEntityReference(typeof(DataEntityShotVersion), TestConstants.TargetShotVersionId);

			DataApiResponse<DataEntityFilePublish> response = m_api.CreateFilePublish(TestConstants.TargetProjectId, TestConstants.TargetShotId, TestConstants.TargetShotVersionId, attributes).Result;
			Assert.IsFalse(response.IsError);

			//DataApiResponse<DataEntityShotVersion> targetShot = m_api.FindShotVersionById(TestConstants.TargetShotVersionId).Result;
			//Assert.IsFalse(targetShot.IsError);

			//targetShot.ResultData!.PathToFrames = targetPath;
			//DataApiResponseGeneric shotVersionUpdate = targetShot.ResultData!.ChangeTracker.CommitChanges(m_api).Result;
			//Assert.IsFalse(shotVersionUpdate.IsError);
		}

		[TestMethod]
		public void CreateShotVersion()
		{
			DataEntityShotVersion attributes = new DataEntityShotVersion();
			attributes.Flagged = false;
			attributes.DataWranglerMeta = "{\"dummy\": \"Created by unit tests\"}";
			attributes.ShotVersionName = "Unit Test Created Shot Version";
			DataApiResponse<DataEntityShotVersion> result = m_api.CreateNewShotVersion(TestConstants.TargetProjectId, TestConstants.TargetShotId, attributes).Result;
			Assert.IsFalse(result.IsError);
		}

		[TestMethod]
		public void GetSpecificFilePublishType()
		{
			DataApiResponse<DataEntityPublishedFileType> response = m_api.FindPublishedFileTypeByCode("video").Result;

			Assert.IsFalse(response.IsError);

		}

		[TestMethod]
		public void GetLocalStorages()
		{
			DataApiResponse<DataEntityLocalStorage[]> storages = m_api.GetLocalStorages().Result;

			Assert.IsFalse(storages.IsError);
		}

		//[TestMethod]
		//public void GetProjectActivityStream()
		//{
		//	int updateLimit = 25;
		//	DataApiResponse<ShotGridEntityActivityStreamResponse> activityStream = m_api.GetEntityActivityStream(ShotGridEntityTypeInfo.Project, TestConstants.TargetProjectId, updateLimit).Result;

		//	Assert.IsFalse(activityStream.IsError);
		//	Assert.IsTrue(activityStream.ResultData!.Updates.Length == updateLimit);
		//}

		//PdG 2023-02-20: Disabled because Activity streams don't provide the expected functionality. Only entity creation / deletion is marked in the project.
		//[TestMethod]
		//public void CheckActivityStreamWithUpdate()
		//{
		//	DataApiResponse<ShotGridEntityActivityStreamResponse> beginActivityStream = m_api.GetEntityActivityStream(ShotGridEntityTypeInfo.Project, TestConstants.TargetProjectId, 1).Result;
		//	Assert.IsFalse(beginActivityStream.IsError);

		//	Dictionary<string, object> valuesToChange = new Dictionary<string, object>
		//	{
		//		["description"] = $"Test change by unit test at {DateTime.Now.ToString(CultureInfo.InvariantCulture)}"
		//	};
		//	DataApiResponseGeneric changeResponse = m_api.UpdateEntityProperties(ShotGridEntityTypeInfo.ShotVersion, TestConstants.TargetShotVersionId, valuesToChange).Result;
		//	Assert.IsFalse(changeResponse.IsError);

		//	DataApiResponse<ShotGridEntityActivityStreamResponse> changeActivity = m_api.GetEntityActivityStream(ShotGridEntityTypeInfo.Project, TestConstants.TargetProjectId, 25, beginActivityStream.ResultData!.LatestUpdateId).Result;
		//	Assert.IsFalse(changeActivity.IsError);
		//	Assert.IsTrue(changeActivity.ResultData!.Updates.Length > 0);

		//	bool foundUpdate = false;
		//	foreach (ShotGridEntityActivityStreamResponse.ShotGridActivityUpdate update in changeActivity.ResultData.Updates)
		//	{
		//		if (update.PrimaryEntity!.EntityType == ShotGridEntityTypeInfo.ShotVersion.CamelCase 
		//		    && update.PrimaryEntity.Id == TestConstants.TargetShotVersionId 
		//		    && update.UpdateType == EActivityUpdateType.Update)
		//		{
		//			foundUpdate = true;
		//		}
		//	}

		//	Assert.IsTrue(foundUpdate);
		//}

		//[TestMethod]
		//public void CreateAttachment()
		//{
		//	ShotGridAPI api = new ShotGridAPI();
		//	ShotGridLoginResponse loginResponse = api.TryLogin(CredentialProvider.Username!, CredentialProvider.Password!).Result;
		//	Assert.IsTrue(loginResponse.Success);

		//	DataApiResponse<ShotGridEntityAttachment> storages = api.CreateFileAttachment(TargetProjectId, TargetPublishFile);

		//	Assert.IsFalse(storages.IsError);
		//}
	}
}