
using DataApiCommon;
using DataApiSFTP;

namespace DataApiTests
{
	[TestClass]
	public class DataApiSFTPTests
	{
		private DataApiSFTPFileSystem m_api = new DataApiSFTPFileSystem();

		[TestInitialize]
		public void ConnectToApi()
		{
			Assert.IsTrue(m_api.Connect(TestConstants.TargetHost, TestConstants.TargetUser, TestConstants.TargetKeyFile));
		}

		[TestCleanup]
		public void Disconnect()
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
		}

		[TestMethod]
		public void GetShotsForProject()
		{
			DataApiResponse<DataEntityProject[]> projects = m_api.GetActiveProjects().Result;
			DataApiResponse<DataEntityShot[]> shots = m_api.GetShotsForProject(TestConstants.TargetProjectId).Result;

			Assert.IsFalse(shots.IsError);
			Assert.IsTrue(shots.ResultData.Length > 0);
			Assert.IsTrue(m_api.LocalCache.GetEntitiesByType<DataEntityShot>().Length == shots.ResultData.Length);
		}
	}
}
