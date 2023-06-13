using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Text;
using CommonLogging;
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

		private static readonly Guid LocalStorageGuid = Guid.Parse("00c0ff1e-1329-4357-82ec-000000000001");


		private static readonly DataEntityPublishedFileType[] PublishedFileTypes = new[]
		{
			new DataEntityPublishedFileType{ EntityId = Guid.Parse("00c0ff1e-1329-4357-82ed-000000000001"), FileType = "video"},
			new DataEntityPublishedFileType{ EntityId = Guid.Parse("00c0ff1e-1329-4357-82ed-000000000002"), FileType = "audio"},
			new DataEntityPublishedFileType{ EntityId = Guid.Parse("00c0ff1e-1329-4357-82ed-000000000003"), FileType = "motion-data"},
		};

		private const string ProjectMetaFileName = "IngestinatorDataApiMeta.json";
		private const string ShotMetaFileName = "IngestinatorShotMeta.json";
		private const string ShotVersionMetaFileName = "IngestinatorShotVersionMeta.json";

		private SftpClient? m_client = null;

		public bool Connect(DataApiSFTPConfig a_config)
		{
			if (a_config.SFTPKeyFile == null)
			{
				Logger.LogError("SFTPApi", "Key file not valid for SFTP connection");
				return false;
			}

			return Connect(a_config.TargetHost, a_config.SFTPUserName, a_config.SFTPKeyFile);
		}

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

		private StringBuilder GetShotIngestFolderPath(DataEntityProject a_project, DataEntityShot a_shot)
		{
			StringBuilder sb = GetProjectIngestFolderPath(a_project);
			sb.Append(a_shot.ShotName);
			sb.Append('/');
			return sb;
		}

		private StringBuilder GetShotVersionIngestFolderPath(DataEntityProject a_project, DataEntityShot a_shot, DataEntityShotVersion a_shotVersion)
		{
			StringBuilder sb = GetShotIngestFolderPath(a_project, a_shot);
			sb.Append(a_shotVersion.ShotVersionName);
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
						string metaPath = directoryEntry.FullName + "/" + ProjectMetaFileName;
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

		public override Task<DataApiResponse<DataEntityFilePublish>> CreateFilePublish(Guid a_projectId, Guid a_shotId, Guid a_shotVersionId, DataEntityFilePublish a_publishData)
		{
			return Task.Run(() =>
			{
				if (m_client == null || !m_client.IsConnected)
				{
					return new DataApiResponse<DataEntityFilePublish>(null, new DataApiErrorDetails("SFTP client is not connected (yet)."));
				}

				DataEntityProject? project = LocalCache.FindEntityById<DataEntityProject>(a_projectId);
				if (project == null)
				{
					return new DataApiResponse<DataEntityFilePublish>(null, new DataApiErrorDetails($"Project with Id {a_projectId} was not found in the local cache"));
				}

				DataEntityShot? shot = LocalCache.FindEntityById<DataEntityShot>(a_shotId);
				if (shot == null)
				{
					return new DataApiResponse<DataEntityFilePublish>(null, new DataApiErrorDetails($"Shot with Id {a_shotId} was not found in the local cache"));
				}

				if (string.IsNullOrEmpty(a_publishData.RelativePathToStorageRoot))
				{
					return new DataApiResponse<DataEntityFilePublish>(null, new DataApiErrorDetails($"File publish was created with invalid relative path: \"{a_publishData.RelativePathToStorageRoot}\""));
				}

				DataEntityFilePublish publish = a_publishData;
				//TODO: Store this file publish information somewhere sane.

				publish.EntityId = Guid.NewGuid();
				LocalCache.AddCachedEntity(publish);

				return new DataApiResponse<DataEntityFilePublish>(a_publishData, null);
			});
		}

		public override Task<DataApiResponse<DataEntityShot>> CreateNewShot(Guid a_projectId, DataEntityShot a_shotData)
		{
			return Task.Run(() =>
			{
				if (m_client == null || !m_client.IsConnected)
				{
					return new DataApiResponse<DataEntityShot>(null, new DataApiErrorDetails($"SFTP client is not connected (yet)."));
				}

				DataEntityProject? project = LocalCache.FindEntityById<DataEntityProject>(a_projectId);
				if (project == null)
				{
					return new DataApiResponse<DataEntityShot>(null, new DataApiErrorDetails($"Project with Id {a_projectId} was not found in the local cache"));
				}

				string outputFolderPath = GetShotIngestFolderPath(project, a_shotData).ToString();
				if (m_client.Exists(outputFolderPath))
				{
					return new DataApiResponse<DataEntityShot>(null, new DataApiErrorDetails($"Shot with name {a_shotData.ShotName} already exists"));
				}

				CreateRemoteDirectoryRecursively(m_client, outputFolderPath);

				DataApiSFTPShotAttributes attrib = new DataApiSFTPShotAttributes(a_shotData)
				{
					EntityId = Guid.NewGuid()
				};
				string metaAsString = JsonConvert.SerializeObject(attrib, Formatting.Indented);
				m_client.WriteAllText(outputFolderPath + "/" + ShotMetaFileName, metaAsString);

				return new DataApiResponse<DataEntityShot>(attrib.ToDataEntity(project), null);
			});
		}

		public override Task<DataApiResponse<DataEntityShotVersion>> CreateNewShotVersion(Guid a_projectId, Guid a_targetShotId, DataEntityShotVersion a_versionData)
		{
			return Task.Run(() => {
				if (m_client == null || !m_client.IsConnected)
				{
					return new DataApiResponse<DataEntityShotVersion>(null, new DataApiErrorDetails("SFTP client is not connected (yet)."));
				}

				DataEntityProject? project = LocalCache.FindEntityById<DataEntityProject>(a_projectId);
				if (project == null)
				{
					return new DataApiResponse<DataEntityShotVersion>(null, new DataApiErrorDetails($"Project with Id {a_projectId} was not found in the local cache"));
				}

				DataEntityShot? shot = LocalCache.FindEntityById<DataEntityShot>(a_targetShotId);
				if (shot == null)
				{
					return new DataApiResponse<DataEntityShotVersion>(null, new DataApiErrorDetails($"Shot with Id {a_targetShotId} was not found in the local cache"));
				}

				if (string.IsNullOrEmpty(a_versionData.ShotVersionName))
				{
					return new DataApiResponse<DataEntityShotVersion>(null, new DataApiErrorDetails($"Shot version was created with invalid name: \"{a_versionData.ShotVersionName}\""));
				}
				
				string shotVersionPath = GetShotVersionIngestFolderPath(project, shot, a_versionData).ToString();
				CreateRemoteDirectoryRecursively(m_client, shotVersionPath);

				DataApiSFTPShotVersionAttributes attribs = new DataApiSFTPShotVersionAttributes(a_versionData)
				{
					EntityId = Guid.NewGuid()
				};
				string metaAsString = JsonConvert.SerializeObject(attribs, Formatting.Indented);
				m_client.WriteAllText(shotVersionPath + "/" + ShotVersionMetaFileName, metaAsString);

				DataEntityShotVersion version = attribs.ToDataEntity(project, shot);
				LocalCache.AddCachedEntity(version);
				return new DataApiResponse<DataEntityShotVersion>(version, null);
			});
		}

		public override Task<DataApiResponse<DataEntityLocalStorage[]>> GetLocalStorages()
		{
			return Task.FromResult(new DataApiResponse<DataEntityLocalStorage[]>(new DataEntityLocalStorage[] { 
				new() {
					EntityId = LocalStorageGuid,
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
				if (m_client.Exists(ingestFolderPath))
				{
					foreach (SftpFile file in m_client.ListDirectory(ingestFolderPath))
					{
						if (file.IsDirectory && !file.Name.StartsWith('.'))
						{
							string metaPath = file.FullName + "/" + ShotMetaFileName;
							DataApiSFTPShotAttributes? attrib = null;
							if (m_client.Exists(metaPath))
							{
								string metaAsString = m_client.ReadAllText(metaPath);
								attrib = JsonConvert.DeserializeObject<DataApiSFTPShotAttributes>(metaAsString);
							}

							if (attrib == null)
							{
								attrib = new DataApiSFTPShotAttributes(file.Name);
								string metaAsString = JsonConvert.SerializeObject(attrib, Formatting.Indented);
								m_client.WriteAllText(metaPath, metaAsString);
							}

							DataEntityShot shot = attrib.ToDataEntity(project);
							LocalCache.AddCachedEntity(shot);
							shots.Add(shot);
						}
					}
				}

				return new DataApiResponse<DataEntityShot[]>(shots.ToArray(), null);
			});
		}

		public override Task<DataApiResponse<DataEntityPublishedFileType[]>> GetPublishedFileTypes()
		{
			return Task.FromResult(new DataApiResponse<DataEntityPublishedFileType[]>(PublishedFileTypes, null));
		}

		public override Task<DataApiResponse<DataEntityShotVersion[]>> GetVersionsForShot(Guid a_shotEntityId)
		{
			return Task.Run(() =>
			{
				if (m_client == null || !m_client.IsConnected)
				{
					return new DataApiResponse<DataEntityShotVersion[]>(null, new DataApiErrorDetails($"SFTP client is not connected (yet)."));
				}

				DataEntityShot? shotData = LocalCache.FindEntityById<DataEntityShot>(a_shotEntityId);
				if (shotData == null)
				{
					return new DataApiResponse<DataEntityShotVersion[]>(null, new DataApiErrorDetails($"Shot {a_shotEntityId} is not known by the local data cache."));
				}

				if (shotData.EntityRelationships.Project == null)
				{
					return new DataApiResponse<DataEntityShotVersion[]>(null, new DataApiErrorDetails($"Shot {a_shotEntityId} does not specify a ."));
				}

				DataEntityProject? project = LocalCache.FindEntityById<DataEntityProject>(shotData.EntityRelationships.Project!.EntityId);
				if (project == null)
				{
					return new DataApiResponse<DataEntityShotVersion[]>(null, new DataApiErrorDetails($"Shot {a_shotEntityId} is not known by the local data cache."));
				}

				List<DataEntityShotVersion> versions = new List<DataEntityShotVersion>();
				string ingestFolderPath = GetShotIngestFolderPath(project, shotData).ToString();
				if (m_client.Exists(ingestFolderPath))
				{
					foreach (SftpFile file in m_client.ListDirectory(ingestFolderPath))
					{
						if (file.IsDirectory && !file.Name.StartsWith('.'))
						{
							string metaPath = file.FullName + "/" + ShotVersionMetaFileName;
							DataApiSFTPShotVersionAttributes? attrib = null;
							if (m_client.Exists(metaPath))
							{
								string metaAsString = m_client.ReadAllText(metaPath);
								attrib = JsonConvert.DeserializeObject<DataApiSFTPShotVersionAttributes>(metaAsString);
							}

							if (attrib == null)
							{
								attrib = new DataApiSFTPShotVersionAttributes();
								string metaAsString = JsonConvert.SerializeObject(attrib, Formatting.Indented);
								m_client.WriteAllText(metaPath, metaAsString);
							}

							DataEntityShotVersion shotVersion = new DataEntityShotVersion
							{
								EntityId = attrib.EntityId,
								ShotVersionName = file.Name,
								EntityRelationships =
								{
									Project = new DataEntityReference(project),
									Parent = new DataEntityReference(shotData)
								},
								DataWranglerMeta = attrib.DataWranglerMeta,
								Description = attrib.Description,
								Flagged = attrib.Flagged,
								ImageURL = attrib.ImageURL
							};
							LocalCache.AddCachedEntity(shotVersion);
							versions.Add(shotVersion);
						}
					}
				}

				return new DataApiResponse<DataEntityShotVersion[]>(versions.ToArray(), null);
			});
		}

		public override Task<DataApiResponseGeneric> UpdateEntityProperties(DataEntityBase a_targetEntity, Dictionary<PropertyInfo, object?> a_changedValues)
		{
			return Task.Run(() =>
			{
				if (m_client == null || !m_client.IsConnected)
				{
					return new DataApiResponseGeneric(null, new DataApiErrorDetails("SFTP Client is not connected (yet)"));
				}

				switch (a_targetEntity)
				{
					case DataEntityProject dataEntityProject:
						return UpdateProjectEntityProperties(m_client, dataEntityProject, a_changedValues);
					case DataEntityShot shotEntity:
						return UpdateShotEntityProperties(m_client, shotEntity, a_changedValues);
					case DataEntityShotVersion shotVersionEntity:
						return UpdateShotVersionEntityProperties(m_client, shotVersionEntity, a_changedValues);
					default:
						return new DataApiResponseGeneric(null, new DataApiErrorDetails($"Unknown data entity type to update: {a_targetEntity.GetType()}"));
				}
			});
		}

		private DataApiResponseGeneric UpdateProjectEntityProperties(SftpClient a_client, DataEntityProject a_dataEntityProject, Dictionary<PropertyInfo, object?> a_changedValues)
		{
			string targetFilePath = GetProjectFolderPath(a_dataEntityProject).ToString();
			string metaPath = targetFilePath + "/" + ProjectMetaFileName;
			if (!a_client.Exists(metaPath))
			{
				return new DataApiResponseGeneric(null, new DataApiErrorDetails($"Tried to update project with name {a_dataEntityProject.Name} which does not have a valid meta file"));
			}

			DataApiSFTPProjectAttributes? attrib = JsonConvert.DeserializeObject<DataApiSFTPProjectAttributes>(a_client.ReadAllText(metaPath));
			if (attrib == null)
			{
				return new DataApiResponseGeneric(null, new DataApiErrorDetails($"Failed to deserialize project attributes. Attribute content: {a_client.ReadAllText(metaPath)}"));
			}

			DataEntityProject project = attrib.ToDataEntity();
			foreach(KeyValuePair<PropertyInfo, object?> kvp in a_changedValues)
			{
				kvp.Key.SetValue(project, kvp.Value);
			}

			attrib = new DataApiSFTPProjectAttributes(project);
			a_client.WriteAllText(metaPath, JsonConvert.SerializeObject(attrib, Formatting.Indented));

			return new DataApiResponseGeneric(project, null);
		}

		private DataApiResponseGeneric UpdateShotEntityProperties(SftpClient a_client, DataEntityShot a_shotEntity, Dictionary<PropertyInfo, object?> a_changedValues)
		{
			DataEntityProject? project = LocalCache.FindEntityById<DataEntityProject>(a_shotEntity.EntityRelationships.Project!.EntityId);
			if (project == null)
			{
				return new DataApiResponseGeneric(null, new ($"Could not find project with guid {a_shotEntity.EntityRelationships.Project!.EntityId} in local cache"));
			}

			string targetFilePath = GetShotIngestFolderPath(project, a_shotEntity).ToString();
			string metaPath = targetFilePath + "/" + ShotMetaFileName;
			if (!a_client.Exists(metaPath))
			{
				return new DataApiResponseGeneric(null, new DataApiErrorDetails($"Tried to update shot with name {a_shotEntity.ShotName} which does not have a valid meta file"));
			}

			DataApiSFTPShotAttributes? attrib = JsonConvert.DeserializeObject<DataApiSFTPShotAttributes>(a_client.ReadAllText(metaPath));
			if (attrib == null)
			{
				return new DataApiResponseGeneric(null, new DataApiErrorDetails($"Failed to deserialize project attributes. Attribute content: {a_client.ReadAllText(metaPath)}"));
			}

			DataEntityShot shot = attrib.ToDataEntity(project);
			foreach (KeyValuePair<PropertyInfo, object?> kvp in a_changedValues)
			{
				kvp.Key.SetValue(shot, kvp.Value);
			}

			attrib = new DataApiSFTPShotAttributes(shot);
			a_client.WriteAllText(metaPath, JsonConvert.SerializeObject(attrib, Formatting.Indented));

			return new DataApiResponseGeneric(shot, null);
		}

		private DataApiResponseGeneric UpdateShotVersionEntityProperties(SftpClient a_client, DataEntityShotVersion a_shotVersionEntity, Dictionary<PropertyInfo, object?> a_changedValues)
		{
			DataEntityProject? project = LocalCache.FindEntityById<DataEntityProject>(a_shotVersionEntity.EntityRelationships.Project!.EntityId);
			if (project == null)
			{
				return new DataApiResponseGeneric(null, new($"Could not find project with guid {a_shotVersionEntity.EntityRelationships.Project!.EntityId} in local cache"));
			}

			DataEntityShot? shot = LocalCache.FindEntityById<DataEntityShot>(a_shotVersionEntity.EntityRelationships.Parent!.EntityId);
			if (shot == null)
			{
				return new DataApiResponseGeneric(null, new($"Could not find project with guid {a_shotVersionEntity.EntityRelationships.Parent!.EntityId} in local cache"));
			}

			string targetFilePath = GetShotVersionIngestFolderPath(project, shot, a_shotVersionEntity).ToString();
			string metaPath = targetFilePath + "/" + ShotVersionMetaFileName;
			if (!a_client.Exists(metaPath))
			{
				return new DataApiResponseGeneric(null, new DataApiErrorDetails($"Tried to update shot version with name {a_shotVersionEntity.ShotVersionName} which does not have a valid meta file"));
			}

			DataApiSFTPShotVersionAttributes? attrib = JsonConvert.DeserializeObject<DataApiSFTPShotVersionAttributes>(a_client.ReadAllText(metaPath));
			if (attrib == null)
			{
				return new DataApiResponseGeneric(null, new DataApiErrorDetails($"Failed to deserialize project attributes. Attribute content: {a_client.ReadAllText(metaPath)}"));
			}

			DataEntityShotVersion shotVersion = attrib.ToDataEntity(project, shot);
			foreach (KeyValuePair<PropertyInfo, object?> kvp in a_changedValues)
			{
				kvp.Key.SetValue(shotVersion, kvp.Value);
			}

			attrib = new DataApiSFTPShotVersionAttributes(shotVersion);
			a_client.WriteAllText(metaPath, JsonConvert.SerializeObject(attrib, Formatting.Indented));

			return new DataApiResponseGeneric(shotVersion, null);
		}

		private void CreateRemoteDirectoryRecursively(SftpClient a_client, string a_targetDirectory)
		{
			int lastIndex = 0;
			do
			{
				lastIndex = a_targetDirectory.IndexOf('/', lastIndex + 1);
				string path = a_targetDirectory.Substring(0, (lastIndex == -1) ? a_targetDirectory.Length : lastIndex);
				if (!a_client.Exists(path) || !a_client.GetAttributes(path).IsDirectory)
				{
					a_client.CreateDirectory(path);
				}
			} while (lastIndex != -1);
		}
	}
}