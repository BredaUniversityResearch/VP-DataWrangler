using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShotGridIntegration;

namespace ShotGridIntegrationTest
{
	[TestClass]
	public class ShotGridApiTests
	{
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

			ShotGridEntityProject[]? projects = api.GetProjects().Result;
			
			Assert.IsTrue(projects != null);

			foreach (ShotGridEntityProject project in projects!)
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

			ShotGridEntityShot[]? shots = api.GetShotsForProject(285).Result;

			Assert.IsTrue(shots != null);

			foreach (ShotGridEntityShot shot in shots!)
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

			ShotGridEntityShotVersion[]? shotVersions = api.GetVersionsForShot(1369).Result;

			Assert.IsTrue(shotVersions != null);

			foreach (ShotGridEntityShotVersion shotVersion in shotVersions!)
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

			ShotGridEntityShot[]? shots = api.GetShotsForProject(285).Result;

			Assert.IsTrue(shots != null);

			foreach (ShotGridEntityShot shot in shots!)
			{
				Assert.IsTrue(shot.Attributes.ShotCode != null);
				Assert.IsTrue(shot.Links.Self != null);
			}
		}
	}
}