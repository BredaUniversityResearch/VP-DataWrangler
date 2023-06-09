﻿using System;
using System.Threading.Tasks;
using DataApiCommon;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ShotGridIntegration;

namespace ShotGridIntegrationTest
{
	[TestClass]
	public class ShotGridChangeTrackerTest
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

			ShotGridEntityShotVersion versionEntity = new ShotGridEntityShotVersion();
			versionEntity.Attributes.Flagged = true;

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
			ShotGridEntityShotVersion? changedShotVersion = commitChangeTask.Result.ResultDataGeneric as ShotGridEntityShotVersion;
			Assert.IsTrue(changedShotVersion != null);
			Assert.IsTrue(changedShotVersion!.Attributes.Flagged == newFlaggedValue);
		}
	}
}
