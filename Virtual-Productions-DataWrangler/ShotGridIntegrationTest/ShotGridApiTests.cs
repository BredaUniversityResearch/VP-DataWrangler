using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
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
			ShotGridAPIResponse<ShotGridEntityProject[]> projects = m_api.GetActiveProjects().Result;

			Assert.IsFalse(projects.IsError);

			foreach (ShotGridEntityProject project in projects.ResultData!)
			{
				Assert.IsTrue(project.Attributes.Name != null);
				Assert.IsTrue(project.Links.Self != null);
			}
		}

		[TestMethod]
		public void GetShots()
		{
			ShotGridAPIResponse<ShotGridEntityShot[]> shots = m_api.GetShotsForProject(TestConstants.TargetProjectId).Result;

			Assert.IsFalse(shots.IsError);
			Assert.IsTrue(shots.ResultData!.Length > 0);

			foreach (ShotGridEntityShot shot in shots.ResultData!)
			{
				Assert.IsTrue(!string.IsNullOrEmpty(shot.Attributes.ShotCode));
				Assert.IsTrue(shot.Links.Self != null);
			}
		}

		[TestMethod]
		public void GetShotsAndTestCache()
		{
			ShotGridAPIResponse<ShotGridEntityShot[]> shots = m_api.GetShotsForProject(TestConstants.TargetProjectId).Result;

			Assert.IsFalse(shots.IsError);
			Assert.IsTrue(shots.ResultData!.Length > 0);

			foreach (ShotGridEntityShot shot in shots.ResultData!)
			{
				Assert.IsTrue(!string.IsNullOrEmpty(shot.Attributes.ShotCode));
				Assert.IsTrue(shot.Links.Self != null);
			}

			ShotGridEntityShot[] cachedShots = m_api.LocalCache.FindEntities<ShotGridEntityShot>(ShotGridSimpleSearchFilter.ForProject(TestConstants.TargetProjectId));
			Assert.IsTrue(cachedShots.Length == shots.ResultData.Length);
			ShotGridEntityShot[] mismatchFilterShots = m_api.LocalCache.FindEntities<ShotGridEntityShot>(ShotGridSimpleSearchFilter.ForProject(-1));
			Assert.IsTrue(mismatchFilterShots.Length == 0);
		}

		[TestMethod]
		public void GetVersionsForShot()
		{
			ShotGridAPIResponse<ShotGridEntityShotVersion[]> shotVersions = m_api.GetVersionsForShot(TestConstants.TargetShotId).Result;

			Assert.IsFalse(shotVersions.IsError);

			foreach (ShotGridEntityShotVersion shotVersion in shotVersions.ResultData!)
			{
				Assert.IsTrue(!string.IsNullOrEmpty(shotVersion.Attributes.VersionCode));
				Assert.IsTrue(shotVersion.Links.Self != null);
			}
		}

		[TestMethod]
		public void GetSortedVersionsForShot()
		{
			ShotGridAPIResponse<ShotGridEntityShotVersion[]> shotVersions = m_api.GetVersionsForShot(TestConstants.TargetShotId, new ShotGridSortSpecifier("code", false)).Result;

			Assert.IsFalse(shotVersions.IsError);

			foreach (ShotGridEntityShotVersion shotVersion in shotVersions.ResultData!)
			{
				Assert.IsTrue(!string.IsNullOrEmpty(shotVersion.Attributes.VersionCode));
				Assert.IsTrue(shotVersion.Links.Self != null);
			}
		}

		[TestMethod]
		public void GetPublishesSchema()
		{
			ShotGridAPIResponse<ShotGridEntityFieldSchema[]> schemas = m_api.GetEntityFieldSchema(ShotGridEntityName.Shot.CamelCase, TestConstants.TargetProjectId).Result;
			Assert.IsFalse(schemas.IsError);
			Assert.IsTrue(schemas.ResultData!.Length > 0);
		}

		[TestMethod]
		public void GetPublishesForShot()
		{
			ShotGridAPIResponse<ShotGridEntityFilePublish[]> shots = m_api.GetPublishesForShotVersion(TestConstants.TargetShotVersionId).Result;

			Assert.IsFalse(shots.IsError);
		}

		[TestMethod]
		public void UpdateEntityData()
		{
			ShotGridAPIResponse<ShotGridEntityShotVersion> response = m_api.UpdateEntityProperties<ShotGridEntityShotVersion>(
				TestConstants.TargetShotVersionId, new Dictionary<string, object> {{"sg_datawrangler_meta", "test"}}).Result;

			Assert.IsFalse(response.IsError);
		}

		[TestMethod]
		public void CreateFilePublish()
		{
			ShotGridAPIResponse<ShotGridEntityRelation?> fileTypeRelation = m_api.FindRelationByCode(ShotGridEntityName.PublishedFileType, "video").Result;
			Assert.IsTrue(fileTypeRelation.ResultData != null);

			string targetPath = "\\\\cradlenas\\Virtual Productions\\Phils VP Pipeline Testing Playground\\Shots\\Shot 01\\Take 08\\A014_05111248_C002.braw";


			ShotGridEntityFilePublish.FilePublishAttributes attributes = new ShotGridEntityFilePublish.FilePublishAttributes();
			attributes.Path = new ShotGridFileLink(new Uri(targetPath));
			attributes.PublishedFileName = "Testing braw File";
			attributes.PublishedFileType = new ShotGridEntityReference(fileTypeRelation.ResultData!.ShotGridType!, fileTypeRelation.ResultData.Id);
			attributes.ShotVersion = new ShotGridEntityReference(ShotGridEntityName.ShotVersion, TestConstants.TargetShotVersionId);

			ShotGridAPIResponse<ShotGridEntityFilePublish> response = m_api.CreateFilePublish(TestConstants.TargetProjectId, TestConstants.TargetShotId, TestConstants.TargetShotVersionId, attributes).Result;
			Assert.IsFalse(response.IsError);

			ShotGridAPIResponse<ShotGridEntityShotVersion> targetShot = m_api.FindShotVersionById(TestConstants.TargetShotVersionId).Result;
			Assert.IsFalse(targetShot.IsError);

			targetShot.ResultData!.Attributes.PathToFrames = targetPath;
			ShotGridAPIResponseGeneric shotVersionUpdate = targetShot.ResultData!.ChangeTracker.CommitChanges(m_api).Result;
			Assert.IsFalse(shotVersionUpdate.IsError);
		}

		[TestMethod]
		public void CreateShotVersion()
		{
			ShotVersionAttributes attributes = new ShotVersionAttributes();
			attributes.Flagged = false;
			attributes.DataWranglerMeta = "{\"dummy\": \"Created by unit tests\"}";
			attributes.VersionCode = "Unit Test Created Shot Version";
			ShotGridAPIResponse<ShotGridEntityShotVersion> result = m_api.CreateNewShotVersion(TestConstants.TargetProjectId, TestConstants.TargetShotId, attributes).Result;
			Assert.IsFalse(result.IsError);
		}

		[TestMethod]
		public void GetFilePublishTypes()
		{
			ShotGridAPIResponse<ShotGridEntityRelation[]> response = m_api.GetPublishFileTypes().Result;

			Assert.IsFalse(response.IsError);

		}

		[TestMethod]
		public void GetLocalStorages()
		{
			ShotGridAPIResponse<ShotGridEntityLocalStorage[]> storages = m_api.GetLocalStorages().Result;

			Assert.IsFalse(storages.IsError);
		}

		[TestMethod]
		public void GetProjectActivityStream()
		{
			int updateLimit = 25;
			ShotGridAPIResponse<ShotGridEntityActivityStreamResponse> activityStream = m_api.GetEntityActivityStream(ShotGridEntityName.Project, TestConstants.TargetProjectId, updateLimit).Result;

			Assert.IsFalse(activityStream.IsError);
			Assert.IsTrue(activityStream.ResultData!.Updates.Length == updateLimit);
		}

		//PdG 2023-02-20: Disabled because Activity streams don't provide the expected functionality. Only entity creation / deletion is marked in the project.
		//[TestMethod]
		//public void CheckActivityStreamWithUpdate()
		//{
		//	ShotGridAPIResponse<ShotGridEntityActivityStreamResponse> beginActivityStream = m_api.GetEntityActivityStream(ShotGridEntityName.Project, TestConstants.TargetProjectId, 1).Result;
		//	Assert.IsFalse(beginActivityStream.IsError);

		//	Dictionary<string, object> valuesToChange = new Dictionary<string, object>
		//	{
		//		["description"] = $"Test change by unit test at {DateTime.Now.ToString(CultureInfo.InvariantCulture)}"
		//	};
		//	ShotGridAPIResponseGeneric changeResponse = m_api.UpdateEntityProperties(ShotGridEntityName.ShotVersion, TestConstants.TargetShotVersionId, valuesToChange).Result;
		//	Assert.IsFalse(changeResponse.IsError);

		//	ShotGridAPIResponse<ShotGridEntityActivityStreamResponse> changeActivity = m_api.GetEntityActivityStream(ShotGridEntityName.Project, TestConstants.TargetProjectId, 25, beginActivityStream.ResultData!.LatestUpdateId).Result;
		//	Assert.IsFalse(changeActivity.IsError);
		//	Assert.IsTrue(changeActivity.ResultData!.Updates.Length > 0);

		//	bool foundUpdate = false;
		//	foreach (ShotGridEntityActivityStreamResponse.ShotGridActivityUpdate update in changeActivity.ResultData.Updates)
		//	{
		//		if (update.PrimaryEntity!.EntityType == ShotGridEntityName.ShotVersion.CamelCase 
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

		//	ShotGridAPIResponse<ShotGridEntityAttachment> storages = api.CreateFileAttachment(TargetProjectId, TargetPublishFile);

		//	Assert.IsFalse(storages.IsError);
		//}
	}
}