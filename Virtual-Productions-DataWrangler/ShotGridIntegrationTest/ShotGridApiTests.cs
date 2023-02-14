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

			foreach (ShotGridEntityShot shot in shots.ResultData!)
			{
				Assert.IsTrue(!string.IsNullOrEmpty(shot.Attributes.ShotCode));
				Assert.IsTrue(shot.Links.Self != null);
			}
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

		//2022-12-09 PdG: Was used as a utility function, now fails the test due to not being able to deserialize. The tested function is currently not being used other than for debugging the layout of an object.
		//[TestMethod]
		//public void GetPublishesSchema()
		//{
		//	ShotGridAPIResponse<ShotGridEntityFieldSchema[]> schemas = m_api.GetEntityFieldSchema("Attachment", TargetProjectId).Result;
		//	Assert.IsFalse(schemas.IsError);
		//	Assert.IsTrue(schemas.ResultData!.Length > 0);
		//}

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
				TestConstants.TargetShotVersionId, new Dictionary<string, object>{{"sg_datawrangler_meta", "test"}}).Result;

			Assert.IsFalse(response.IsError);
		}

		[TestMethod]
		public void CreateFilePublish()
		{
			ShotGridAPIResponse<ShotGridEntityRelation?> fileTypeRelation = m_api.FindRelationByCode(ShotGridEntityName.PublishedFileType, "video").Result;
			Assert.IsTrue(fileTypeRelation.ResultData != null);

			string targetPath = "file://cradlenas/Projects/VirtualProductions/Phils VP Pipeline Testing Playground\\Shots\\Shot 01\\Take 08\\A014_05111248_C002.braw";


			ShotGridEntityFilePublish.FilePublishAttributes attributes = new ShotGridEntityFilePublish.FilePublishAttributes();
			attributes.Path = new ShotGridEntityFilePublish.FileLink{
				//Url = new UriBuilder { Scheme = Uri.UriSchemeFile, Path = targetPath, Host = "cradlenas" }.Uri.AbsoluteUri,
				FileName = "UNIT_TEST_TEST_FILE.braw",
				LinkType = "local",
				LocalPath = targetPath,
				LocalStorageTarget = new ShotGridEntityReference(ShotGridEntityName.LocalStorage,3)
			};
			attributes.PublishedFileName = "Testing braw File";
			attributes.PublishedFileType = new ShotGridEntityReference(fileTypeRelation.ResultData!.ShotGridType!, fileTypeRelation.ResultData.Id);
			attributes.ShotVersion = new ShotGridEntityReference(ShotGridEntityName.ShotVersion, TestConstants.TargetShotVersionId);

			ShotGridAPIResponse<ShotGridEntityFilePublish> response = m_api.CreateFilePublish(TestConstants.TargetProjectId, TestConstants.TargetShotId, TestConstants.TargetShotVersionId, attributes).Result;

			Assert.IsFalse(response.IsError);

		}

		[TestMethod]
		public void GetFilePublishTypes()
		{
			ShotGridAPIResponse<ShotGridEntityRelation[]> response = m_api.GetPublishFileTypes(TestConstants.TargetProjectId).Result;

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

		[TestMethod]
		public void CheckActivityStreamWithUpdate()
		{
			ShotGridAPIResponse<ShotGridEntityActivityStreamResponse> beginActivityStream = m_api.GetEntityActivityStream(ShotGridEntityName.Project, TestConstants.TargetProjectId, 1).Result;
			Assert.IsFalse(beginActivityStream.IsError);

			Dictionary<string, object> valuesToChange = new Dictionary<string, object>
			{
				["description"] = $"Test change by unit test at {DateTime.Now.ToString(CultureInfo.InvariantCulture)}"
			};
			ShotGridAPIResponseGeneric changeResponse = m_api.UpdateEntityProperties(ShotGridEntityName.ShotVersion, TestConstants.TargetShotVersionId, valuesToChange).Result;
			Assert.IsFalse(changeResponse.IsError);

			ShotGridAPIResponse<ShotGridEntityActivityStreamResponse> changeActivity = m_api.GetEntityActivityStream(ShotGridEntityName.Project, TestConstants.TargetProjectId, 25, beginActivityStream.ResultData!.LatestUpdateId).Result;
			Assert.IsFalse(changeActivity.IsError);
			Assert.IsTrue(changeActivity.ResultData!.Updates.Length > 0);

			bool foundUpdate = false;
			foreach (ShotGridEntityActivityStreamResponse.ShotGridActivityUpdate update in changeActivity.ResultData.Updates)
			{
				if (update.PrimaryEntity!.EntityType == ShotGridEntityName.ShotVersion.CamelCase 
				    && update.PrimaryEntity.Id == TestConstants.TargetShotVersionId 
				    && update.UpdateType == EActivityUpdateType.Update)
				{
					foundUpdate = true;
				}
			}

			Assert.IsTrue(foundUpdate);
		}

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