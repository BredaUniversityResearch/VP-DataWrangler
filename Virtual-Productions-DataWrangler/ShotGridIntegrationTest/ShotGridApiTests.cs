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
		public void GetPublishesForShot()
		{

			ShotGridAPI api = new ShotGridAPI();
			ShotGridLoginResponse loginResponse = api.TryLogin(CredentialProvider.Username!, CredentialProvider.Password!).Result;
			Assert.IsTrue(loginResponse.Success);

			ShotGridAPIResponse<ShotGridEntityShot[]> shots = api.GetShotsForProject(TargetProjectId).Result;

			Assert.IsFalse(shots.IsError);

			foreach (ShotGridEntityShot shot in shots.ResultData!)
			{
				Assert.IsTrue(shot.Attributes.ShotCode != null);
				Assert.IsTrue(shot.Links.Self != null);
			}
		}

		[TestMethod]
		public void UpdateEntityData()
		{
			ShotGridAPI api = new ShotGridAPI();
			ShotGridLoginResponse loginResponse = api.TryLogin(CredentialProvider.Username!, CredentialProvider.Password!).Result;
			Assert.IsTrue(loginResponse.Success);

			ShotGridAPIResponse<ShotGridEntityShotVersion> response = api.UpdateEntityProperties<ShotGridEntityShotVersion>(
				EShotGridEntity.Version, TargetShotVersionId, new Dictionary<string, object>{{"sg_datawrangler_meta", "test"}}).Result;

			Assert.IsFalse(response.IsError);
		}
	}
}