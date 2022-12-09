using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShotGridIntegration;

namespace ShotGridIntegrationTest
{
	[TestClass]
	public class ShotGridApiTests
	{
		private const int TargetProjectId = 285;
		private const int TargetShotId = 1369;
		private const int TargetShotVersionId = 8195;
		private const int TargetPublishFile = 133;

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
			ShotGridAPIResponse<ShotGridEntityShot[]> shots = m_api.GetShotsForProject(TargetProjectId).Result;

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
			ShotGridAPIResponse<ShotGridEntityShotVersion[]> shotVersions = m_api.GetVersionsForShot(TargetShotId).Result;

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
			ShotGridAPIResponse<ShotGridEntityShotVersion[]> shotVersions = m_api.GetVersionsForShot(TargetShotId, new ShotGridSortSpecifier("code", false)).Result;

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
			ShotGridAPIResponse<ShotGridEntityFilePublish[]> shots = m_api.GetPublishesForShotVersion(TargetShotVersionId).Result;

			Assert.IsFalse(shots.IsError);
		}

		[TestMethod]
		public void UpdateEntityData()
		{
			ShotGridAPIResponse<ShotGridEntityShotVersion> response = m_api.UpdateEntityProperties<ShotGridEntityShotVersion>(
				TargetShotVersionId, new Dictionary<string, object>{{"sg_datawrangler_meta", "test"}}).Result;

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
			attributes.ShotVersion = new ShotGridEntityReference(ShotGridEntityName.ShotVersion, TargetShotVersionId);

			ShotGridAPIResponse<ShotGridEntityFilePublish> response = m_api.CreateFilePublish(TargetProjectId, TargetShotId, TargetShotVersionId, attributes).Result;

			Assert.IsFalse(response.IsError);

		}

		[TestMethod]
		public void GetFilePublishTypes()
		{
			ShotGridAPIResponse<ShotGridEntityRelation[]> response = m_api.GetPublishFileTypes(TargetProjectId).Result;

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
			ShotGridAPIResponse<ShotGridEntityActivityStreamResponse> activityStream = m_api.GetProjectActivityStream(TargetProjectId).Result;

			Assert.IsFalse(activityStream.IsError);
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