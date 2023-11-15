
using DataApiCommon;
using DataApiSFTP;
using System;
using System.Reflection;
using BlackmagicCameraControlData;
using CommonLogging;
using DataWranglerCommon.IngestDataSources;

namespace DataApiTests
{
	[TestClass]
	public class DataApiSFTPTests
	{
		private static DataApiSFTPFileSystem m_api = null!;

		[ClassInitialize]
		public static void ConnectToApi(TestContext a_context)
		{
			DataApiSFTPConfig config = new DataApiSFTPConfig(TestConstants.TargetHost, TestConstants.TargetUser, TestConstants.DefaultDataStoreFtpKeyFilePath);
			m_api = new DataApiSFTPFileSystem(config);

			Assert.IsTrue(m_api.StartConnect().Result);
			Logger.Instance.OnMessageLogged += OnLoggerMessageLogged;
		}

		private static void OnLoggerMessageLogged(TimeOnly a_time, string a_source, ELogSeverity a_severity, string a_message)
		{
			if (a_severity == ELogSeverity.Error)
			{
				throw new AssertFailedException($"Received error message: {a_message}");
			}
		}

		[ClassCleanup]
		public static void Disconnect()
		{
			m_api.Dispose();
		}

		[TestMethod]
		public void ConnectToServer()
		{
			//Handled by Initialize / Disconnect
		}

		[TestMethod]
		public void GetActiveProjects()
		{
			DataApiResponse<DataEntityProject[]> projects = m_api.GetActiveProjects().Result;
			Assert.IsFalse(projects.IsError);

			Assert.IsTrue(Array.Find(projects.ResultData, (a_proj) => a_proj.Name == ".." || a_proj.Name == ".") == null);
			Assert.IsTrue(projects.ResultData.Length > 0, "Expected at least one active project");
			Assert.IsTrue(m_api.LocalCache.GetEntitiesByType<DataEntityProject>().Length == projects.ResultData.Length);

			foreach (var val in projects.ResultData)
			{
				Assert.IsFalse(string.IsNullOrEmpty(val.Name));
			}
		}

		[TestMethod]
		public void GetShotsForProject()
		{
			DataApiResponse<DataEntityProject[]> projects = m_api.GetActiveProjects().Result;
			DataApiResponse<DataEntityShot[]> shots = m_api.GetShotsForProject(TestConstants.TargetProjectId).Result;

			Assert.IsFalse(shots.IsError);
			Assert.IsTrue(shots.ResultData.Length > 0);
			Assert.IsTrue(m_api.LocalCache.GetEntitiesByType<DataEntityShot>().Length == shots.ResultData.Length);

			foreach (var val in shots.ResultData)
			{
				Assert.IsFalse(string.IsNullOrEmpty(val.ShotName));
			}
		}

		[TestMethod]
		public void GetShotVersionsForProject()
		{
			DataApiResponse<DataEntityProject[]> projects = m_api.GetActiveProjects().Result;
			DataApiResponse<DataEntityShot[]> shots = m_api.GetShotsForProject(TestConstants.TargetProjectId).Result;
			Assert.IsFalse(shots.IsError);
			Assert.IsTrue(shots.ResultData.Length > 0);

			DataApiResponse<DataEntityShotVersion[]> versions = m_api.GetVersionsForShot(TestConstants.TargetShotId).Result;

			Assert.IsFalse(versions.IsError);
			Assert.IsTrue(versions.ResultData.Length > 0);
			Assert.IsTrue(m_api.LocalCache.GetEntitiesByType<DataEntityShotVersion>().Length == versions.ResultData.Length);

			foreach (var val in versions.ResultData)
			{
				Assert.IsFalse(string.IsNullOrEmpty(val.ShotVersionName));
			}
		}

		[TestMethod]
		public void CreateShotForProject()
		{
			DataEntityShot shotData = new DataEntityShot()
			{
				ShotName = "Unit_test_create_shot_" + DateTimeOffset.UtcNow.ToUnixTimeSeconds()
			};

			DataApiResponse<DataEntityProject[]> projects = m_api.GetActiveProjects().Result;
			DataApiResponse<DataEntityShot> response = m_api.CreateNewShot(TestConstants.TargetProjectId, shotData).Result;

			Assert.IsFalse(response.IsError);
			Assert.IsTrue(response.ResultData.EntityId != Guid.Empty);
		}

		[TestMethod]
		public void CreateShotVersionForProject()
		{
			DataEntityShotVersion shotData = new DataEntityShotVersion()
			{
				ShotVersionName = "Unit_test_create_shot_" + DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
				DataWranglerMeta = "{}",
				Description = "Shot version created by unit test",
				Flagged = true
			};

			DataApiResponse<DataEntityProject[]> projects = m_api.GetActiveProjects().Result;
			DataApiResponse<DataEntityShot[]> shots = m_api.GetShotsForProject(TestConstants.TargetProjectId).Result;
			DataApiResponse<DataEntityShotVersion> response = m_api.CreateNewShotVersion(TestConstants.TargetProjectId, TestConstants.TargetShotId, shotData).Result;

			Assert.IsFalse(response.IsError);
			Assert.IsTrue(response.ResultData.EntityId != Guid.Empty);
		}

		[TestMethod]
		public void CreatePublishForShotVersion()
		{
			DataApiResponse<DataEntityProject[]> projects = m_api.GetActiveProjects().Result;
			Assert.IsFalse(projects.IsError, projects.ErrorInfo?.ToString());
			DataApiResponse<DataEntityShot[]> shots = m_api.GetShotsForProject(TestConstants.TargetProjectId).Result;
			Assert.IsFalse(shots.IsError, shots.ErrorInfo?.ToString());
			DataApiResponse<DataEntityShotVersion[]> shotVersions = m_api.GetVersionsForShot(TestConstants.TargetShotId).Result;
			Assert.IsFalse(shotVersions.IsError, shotVersions.ErrorInfo?.ToString());

			DataEntityFilePublish filePublishData = new DataEntityFilePublish()
			{
				Description = "File publish created by unit test",
				PublishedFileName = "Unit test name",
				PublishedFileType = new DataEntityReference(m_api.GetPublishedFileTypes().Result.ResultData![0]),
				RelativePathToStorageRoot = "Phils VP Pipeline Testing Playground/FilePublishTable.csv",
				StorageRoot = new DataEntityReference(m_api.GetLocalStorages().Result.ResultData![0])
			};

			DataApiResponse<DataEntityFilePublish> response = m_api.CreateFilePublish(TestConstants.TargetProjectId, TestConstants.TargetShotId, TestConstants.TargetShotVersionId, filePublishData).Result;

			Assert.IsFalse(response.IsError, response.ErrorInfo?.ToString());
			Assert.IsTrue(response.ResultData.EntityId != Guid.Empty, "Response data contains empty guid");
		}

		//[TestMethod]
		//public void UpdateProjectEntityData()
		//{
		//	DataApiResponse<DataEntityProject[]> projects = m_api.GetActiveProjects().Result;
		//	Assert.IsFalse(projects.IsError);
		//	Assert.IsTrue(projects.ResultData.Length > 0);

		//	PropertyInfo? targetField = typeof(DataEntityProject).GetProperty(nameof(DataEntityProject.Name), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
		//	if (targetField == null)
		//	{
		//		throw new Exception("Could not find field by name to test update with.");
		//	}

		//	DataApiResponse<DataEntityProject> response = m_api.UpdateEntityProperties<DataEntityProject>(
		//		TestConstants.TargetProjectId, new Dictionary<PropertyInfo, object?> { { targetField, $"Unit test updated this description on {DateTime.Now}" } }).Result;

		//	Assert.IsFalse(response.IsError);
		//}

		[TestMethod]
		public void UpdateShotEntityData()
		{
			DataApiResponse<DataEntityProject[]> projects = m_api.GetActiveProjects().Result;
			DataApiResponse<DataEntityShot[]> shots = m_api.GetShotsForProject(TestConstants.TargetProjectId).Result;
			Assert.IsFalse(shots.IsError);
			Assert.IsTrue(shots.ResultData.Length > 0);

			PropertyInfo? targetField = typeof(DataEntityShot).GetProperty(nameof(DataEntityShot.Description), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
			if (targetField == null)
			{
				throw new Exception("Could not find field by name to test update with.");
			}

			DataApiResponse<DataEntityShot> response = m_api.UpdateEntityProperties<DataEntityShot>(
				TestConstants.TargetShotId, new Dictionary<PropertyInfo, object?> { { targetField, $"Unit test updated this description on {DateTime.Now}" } }).Result;

			Assert.IsFalse(response.IsError);
		}

		[TestMethod]
		public void UpdateShotVersionEntityData()
		{
			DataApiResponse<DataEntityProject[]> projects = m_api.GetActiveProjects().Result;
			DataApiResponse<DataEntityShot[]> shots = m_api.GetShotsForProject(TestConstants.TargetProjectId).Result;
			DataApiResponse<DataEntityShotVersion[]> shotVersions = m_api.GetVersionsForShot(TestConstants.TargetShotId).Result;
			Assert.IsFalse(shotVersions.IsError);
			Assert.IsTrue(shotVersions.ResultData.Length > 0);

			PropertyInfo? targetField = typeof(DataEntityShotVersion).GetProperty(nameof(DataEntityShotVersion.Description), BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
			if (targetField == null)
			{
				throw new Exception("Could not find field by name to test update with.");
			}

			DataApiResponse<DataEntityShotVersion> response = m_api.UpdateEntityProperties<DataEntityShotVersion>(
				TestConstants.TargetShotVersionId, new Dictionary<PropertyInfo, object?> { { targetField, $"Unit test updated this description on {DateTime.Now}" } }).Result;

			Assert.IsFalse(response.IsError);
		}

		[TestMethod]
		public void ShotDataTemplateSerialization()
		{
			DataApiResponse<DataEntityProject[]> projects = m_api.GetActiveProjects().Result;
			DataApiResponse<DataEntityShot[]> shots = m_api.GetShotsForProject(TestConstants.TargetProjectId).Result;

			DataEntityShot? shot = m_api.LocalCache.FindEntityById<DataEntityShot>(TestConstants.TargetShotId);
			Assert.IsTrue(shot != null);

			while (true)
			{
				IngestDataSourceMetaBlackmagicUrsa? ursaMeta = shot.DataSourcesTemplate.FindMetaByType<IngestDataSourceMetaBlackmagicUrsa>();
				if (ursaMeta != null)
				{
					shot.DataSourcesTemplate.RemoveFileSourceInstance(ursaMeta);
				}
				else
				{
					break;
				}
			}

			IngestDataSourceMetaBlackmagicUrsa meta = new IngestDataSourceMetaBlackmagicUrsa();
			meta.CameraNumber = "TestNumber";
			shot.DataSourcesTemplate.AddFileSource(meta);

			Assert.IsTrue(shot.ChangeTracker.HasAnyUncommittedChanges());
			DataApiResponseGeneric updateResponse = shot.ChangeTracker.CommitChanges(m_api).Result;
			Assert.IsFalse(updateResponse.IsError);

			{
				DataApiSFTPFileSystem newApi = new DataApiSFTPFileSystem(DataApiSFTPConfig.DefaultConfig);
				bool connectResult = newApi.StartConnect().Result;
				Assert.IsTrue(connectResult);
				projects = newApi.GetActiveProjects().Result;
				shots = newApi.GetShotsForProject(TestConstants.TargetProjectId).Result;
				shot = newApi.LocalCache.FindEntityById<DataEntityShot>(TestConstants.TargetShotId);

				Assert.IsTrue(shot != null);
				IngestDataSourceMetaBlackmagicUrsa? ursaMeta = shot.DataSourcesTemplate.FindMetaByType<IngestDataSourceMetaBlackmagicUrsa>();
				Assert.IsTrue(ursaMeta != null);
				Assert.IsTrue(ursaMeta.CameraNumber == "TestNumber");
			}
		}
	}
}
