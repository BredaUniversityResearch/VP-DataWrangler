using System.Threading.Tasks;
using DataApiCommon;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShotGridIntegration;

namespace ShotGridIntegrationTest
{
	[TestClass]
	public class DataEntityChangeTrackerTests
	{
		[TestMethod]
		public void TestNotifyPropertyChanged()
		{
			ShotGridEntityShotVersion versionEntity = new ShotGridEntityShotVersion();
			versionEntity.Attributes.Flagged = true;
		}

		[TestMethod]
		public void PropagateChangeBack()
		{
			ShotGridAPI api = new ShotGridAPI();
			ShotGridLoginResponse response = api.TryLogin(CredentialProvider.Username!, CredentialProvider.Password!).Result;
			Assert.IsTrue(response.Success);

			DataApiResponse<DataEntityShotVersion[]> shotVersions = api.GetVersionsForShot(TestConstants.TargetShotId).Result;
			if (shotVersions.IsError)
			{
				throw new AssertFailedException(); //Nullable analysis does not like Assert.IsTrue
			}

			Assert.IsTrue(shotVersions.ResultData.Length > 0);
			DataEntityShotVersion shotVersion = shotVersions.ResultData[0];
			bool newFlaggedValue = !shotVersion.Flagged;
			shotVersion.Flagged = newFlaggedValue;
			Assert.IsTrue(shotVersion.ChangeTracker.HasAnyUncommittedChanges());

			Task<DataApiResponseGeneric> commitChangeTask = shotVersion.ChangeTracker.CommitChanges(api);
			commitChangeTask.Wait();
			Assert.IsTrue(!commitChangeTask.Result.IsError);
			DataEntityShotVersion? changedShotVersion = commitChangeTask.Result.ResultDataGeneric as DataEntityShotVersion;
			Assert.IsTrue(changedShotVersion != null);
			Assert.IsTrue(changedShotVersion!.Flagged == newFlaggedValue);
		}
	}
}
