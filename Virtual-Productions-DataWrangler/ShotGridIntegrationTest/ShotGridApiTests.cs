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

		[TestMethod]
		public void Login()
		{
			ShotGridAPI api = new ShotGridAPI();
			ShotGridLoginResponse response = api.TryLogin(CredentialProvider.Username!, CredentialProvider.Password!).Result;
			Assert.IsTrue(response.Success);
		}

		[TestMethod]
		public void GetProjects()
		{
			ShotGridAPI api = new ShotGridAPI();
			ShotGridLoginResponse loginResponse = api.TryLogin(CredentialProvider.Username!, CredentialProvider.Password!).Result;
			Assert.IsTrue(loginResponse.Success);

			ShotGridAPIResponse<ShotGridEntityProject[]> projects = api.GetActiveProjects().Result;
			
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
			ShotGridAPI api = new ShotGridAPI();
			ShotGridLoginResponse loginResponse = api.TryLogin(CredentialProvider.Username!, CredentialProvider.Password!).Result;
			Assert.IsTrue(loginResponse.Success);

			ShotGridAPIResponse<ShotGridEntityShot[]> shots = api.GetShotsForProject(TargetProjectId).Result;

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

			ShotGridAPI api = new ShotGridAPI();
			ShotGridLoginResponse loginResponse = api.TryLogin(CredentialProvider.Username!, CredentialProvider.Password!).Result;
			Assert.IsTrue(loginResponse.Success);

			ShotGridAPIResponse<ShotGridEntityShotVersion[]> shotVersions = api.GetVersionsForShot(TargetShotId).Result;

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
			ShotGridAPI api = new ShotGridAPI();
			ShotGridLoginResponse loginResponse = api.TryLogin(CredentialProvider.Username!, CredentialProvider.Password!).Result;
			Assert.IsTrue(loginResponse.Success);

			ShotGridAPIResponse<ShotGridEntityFieldSchema[]> schemas = api.GetEntityFieldSchema("Attachment", TargetProjectId).Result;
			Assert.IsFalse(schemas.IsError);
			Assert.IsTrue(schemas.ResultData!.Length > 0);
		}

		[TestMethod]
		public void GetPublishesForShot()
		{

			ShotGridAPI api = new ShotGridAPI();
			ShotGridLoginResponse loginResponse = api.TryLogin(CredentialProvider.Username!, CredentialProvider.Password!).Result;
			Assert.IsTrue(loginResponse.Success);

			ShotGridAPIResponse<ShotGridEntityFilePublish[]> shots = api.GetPublishesForShotVersion(TargetShotVersionId).Result;

			Assert.IsFalse(shots.IsError);
		}

		[TestMethod]
		public void UpdateEntityData()
		{
			ShotGridAPI api = new ShotGridAPI();
			ShotGridLoginResponse loginResponse = api.TryLogin(CredentialProvider.Username!, CredentialProvider.Password!).Result;
			Assert.IsTrue(loginResponse.Success);

			ShotGridAPIResponse<ShotGridEntityShotVersion> response = api.UpdateEntityProperties<ShotGridEntityShotVersion>(
				TargetShotVersionId, new Dictionary<string, object>{{"sg_datawrangler_meta", "test"}}).Result;

			Assert.IsFalse(response.IsError);
		}

		[TestMethod]
		public void CreateFilePublish()
		{
			ShotGridAPI api = new ShotGridAPI();
			ShotGridLoginResponse login = api.TryLogin(CredentialProvider.Username!, CredentialProvider.Password!).Result;
			Assert.IsTrue(login.Success);

			ShotGridAPIResponse<ShotGridEntityRelation?> fileTypeRelation = api.FindRelationByCode(ShotGridEntity.TypeNames.PublishedFileType, "video").Result;
			Assert.IsTrue(fileTypeRelation.ResultData != null);

			string targetPath = "file://cradlenas/Projects/VirtualProductions/Phils VP Pipeline Testing Playground\\Shots\\Shot 01\\Take 08\\A014_05111248_C002.braw";


			ShotGridEntityFilePublish.FilePublishAttributes attributes = new ShotGridEntityFilePublish.FilePublishAttributes();
			attributes.Path = new ShotGridEntityFilePublish.FileLink{
				//Url = new UriBuilder { Scheme = Uri.UriSchemeFile, Path = targetPath, Host = "cradlenas" }.Uri.AbsoluteUri,
				FileName = "UNIT_TEST_TEST_FILE.braw",
				LinkType = "local",
				LocalPath = targetPath,
				LocalStorageTarget = new ShotGridEntityReference("LocalStorage",3)
			};
			attributes.PublishedFileName = "Testing braw File";
			attributes.PublishedFileType = new ShotGridEntityReference(fileTypeRelation.ResultData!.ShotGridType, fileTypeRelation.ResultData.Id);
			attributes.ShotVersion = new ShotGridEntityReference(ShotGridEntity.TypeNames.ShotVersion, TargetShotVersionId);

			ShotGridAPIResponse<ShotGridEntityFilePublish> response = api.CreateFilePublish(TargetProjectId, TargetShotId, TargetShotVersionId, attributes).Result;

			Assert.IsFalse(response.IsError);

		}

		[TestMethod]
		public void GetFilePublishTypes()
		{
			ShotGridAPI api = new ShotGridAPI();
			ShotGridLoginResponse login = api.TryLogin(CredentialProvider.Username!, CredentialProvider.Password!).Result;
			Assert.IsTrue(login.Success);

			ShotGridAPIResponse<ShotGridEntityRelation[]> response = api.GetPublishFileTypes(TargetProjectId).Result;

			Assert.IsFalse(response.IsError);

		}

		[TestMethod]
		public void GetLocalStorages()
		{
			ShotGridAPI api = new ShotGridAPI();
			ShotGridLoginResponse loginResponse = api.TryLogin(CredentialProvider.Username!, CredentialProvider.Password!).Result;
			Assert.IsTrue(loginResponse.Success);

			ShotGridAPIResponse<ShotGridEntityLocalStorage[]> storages = api.GetLocalStorages().Result;

			Assert.IsFalse(storages.IsError);
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