using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using DataApiCommon;
using Newtonsoft.Json;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace DataApiSFTP
{
	public class DataApiSFTPFileSystem: DataApi, IDisposable
	{
		private const string DefaultDataStoreFtpRelativeRoot = "/projects/VirtualProductions/";
		private const string DefaultDataStoreIngestFolderName = "RawFootage";

		private static readonly Guid LocalStorageGuid = Guid.Parse("00c0ff1e-1329-4357-82ec-a88e4c86969d");

		private const int EntityIdLocalStorageStart = 100;
		private const int EntityIdProjectStart = 1000;

		private SftpClient? m_client = null;

		public bool Connect(string a_hostName, string a_userName, PrivateKeyFile a_keyFile)
		{
			if (m_client == null)
			{
				m_client = new SftpClient(a_hostName, 22, a_userName, a_keyFile);
				m_client.Connect();
			}
			else
			{
				throw new Exception("Double connection");
			}

			return m_client.IsConnected;
		}

		public void Dispose()
		{
			m_client?.Dispose();
		}

		private StringBuilder GetProjectFolderPath(DataEntityProject a_project)
		{
			StringBuilder sb = new StringBuilder(96);
			sb.Append(DefaultDataStoreFtpRelativeRoot);
			sb.Append('/');
			sb.Append(a_project.Name);
			sb.Append('/');
			return sb;
		}

		private StringBuilder GetProjectIngestFolderPath(DataEntityProject a_project)
		{
			StringBuilder sb = GetProjectFolderPath(a_project);
			sb.Append(DefaultDataStoreIngestFolderName);
			sb.Append('/');
			return sb;
		}

		public override Task<DataApiResponse<DataEntityProject[]>> GetActiveProjects()
		{
			return Task.Run(() =>
			{
				if (m_client == null || !m_client.IsConnected)
				{
					throw new Exception("Api not connected");
				}

				List<DataEntityProject> projects = new List<DataEntityProject>();
				foreach (SftpFile directoryEntry in m_client.ListDirectory(DefaultDataStoreFtpRelativeRoot))
				{
					if (directoryEntry.Attributes.IsDirectory && !directoryEntry.Name.StartsWith('.'))
					{
						string metaPath = directoryEntry.FullName + "/IngestinatorDataApiMeta.json";
						DataApiSFTPProjectAttributes? attrib = null;
						if (m_client.Exists(metaPath))
						{
							string metaAsString = m_client.ReadAllText(metaPath);
							attrib = JsonConvert.DeserializeObject<DataApiSFTPProjectAttributes>(metaAsString);
						}

						if (attrib == null)
						{
							attrib = new DataApiSFTPProjectAttributes();
							string metaAsString = JsonConvert.SerializeObject(attrib, Formatting.Indented);
							m_client.WriteAllText(metaPath, metaAsString);
						}

						if (attrib.Active)
						{
							DataEntityProject project = new DataEntityProject
							{
								EntityId = attrib.EntityId,
								Name = directoryEntry.Name
							};

							LocalCache.AddCachedEntity(project);
							projects.Add(project);
						}
					}
				}

				return new DataApiResponse<DataEntityProject[]>(projects.ToArray(), null);
			});

		}

		public override Task<DataApiResponse<DataEntityFilePublish>> CreateFilePublish(Guid a_projectId, Guid a_parentId, Guid a_shotVersionId, DataEntityFilePublish a_publishData)
		{
			throw new NotImplementedException();
		}

		public override Task<DataApiResponse<DataEntityShot>> CreateNewShot(Guid a_projectId, DataEntityShot a_gridEntityShotAttributes)
		{
			throw new NotImplementedException();
		}

		public override Task<DataApiResponse<DataEntityShotVersion>> CreateNewShotVersion(Guid a_projectId, Guid a_targetShotId, DataEntityShotVersion a_versionData)
		{
			throw new NotImplementedException();
		}

		public override Task<DataApiResponse<DataEntityLocalStorage[]>> GetLocalStorages()
		{
			return Task.FromResult(new DataApiResponse<DataEntityLocalStorage[]>(new DataEntityLocalStorage[] { 
				new() {
					EntityId = Guid.NewGuid(),
					LocalStorageName = "Cradle Nas",
					StorageRoot = new Uri("file:///cradlenas/Virtual Productions/")
				}
			}, null));
		}

		public override Task<DataApiResponse<DataEntityShot[]>> GetShotsForProject(Guid a_projectId)
		{
			return Task.Run(() =>
			{
				if (m_client == null || !m_client.IsConnected)
				{
					return new DataApiResponse<DataEntityShot[]>(null, new DataApiErrorDetails($"SFTP client is not connected (yet)."));
				}

				DataEntityProject? project = LocalCache.FindEntityById<DataEntityProject>(a_projectId);
				if (project == null)
				{
					return new DataApiResponse<DataEntityShot[]>(null, new DataApiErrorDetails($"Project {a_projectId} is not known by the local data cache."));
				}

				List<DataEntityShot> shots = new List<DataEntityShot>();
				string ingestFolderPath = GetProjectIngestFolderPath(project).ToString();
				foreach (SftpFile file in m_client.ListDirectory(ingestFolderPath))
				{
					if (file.IsDirectory && !file.Name.StartsWith('.'))
					{
						string metaPath = file.FullName + "/IngestinatorShotMeta.json";
						DataApiSFTPShotAttributes? attrib = null;
						if (m_client.Exists(metaPath))
						{
							string metaAsString = m_client.ReadAllText(metaPath);
							attrib = JsonConvert.DeserializeObject<DataApiSFTPShotAttributes>(metaAsString);
						}

						if (attrib == null)
						{
							attrib = new DataApiSFTPShotAttributes();
							string metaAsString = JsonConvert.SerializeObject(attrib, Formatting.Indented);
							m_client.WriteAllText(metaPath, metaAsString);
						}

						DataEntityShot shot = new DataEntityShot
						{
							EntityId = attrib.EntityId,
							ShotName = file.Name,
							EntityRelationships =
							{
								Project = new DataEntityReference(project)
							}
						};
						LocalCache.AddCachedEntity(shot);
						shots.Add(shot);
					}
				}

				return new DataApiResponse<DataEntityShot[]>(shots.ToArray(), null);
			});
		}

		public override Task<DataApiResponse<DataEntityPublishedFileType[]>> GetPublishedFileTypes()
		{
			throw new NotImplementedException();
		}

		public override Task<DataApiResponse<DataEntityShotVersion[]>> GetVersionsForShot(Guid a_shotEntityId)
		{
			throw new NotImplementedException();
		}

		public override Task<DataApiResponseGeneric> UpdateEntityProperties(DataEntityBase a_targetEntity, Dictionary<PropertyInfo, object?> a_changedValues)
		{
			throw new NotImplementedException();
		}
	}
}