using System.Globalization;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using CommonLogging;
using CsvHelper;
using CsvHelper.Configuration;
using DataApiCommon;
using DataApiTests;
using Newtonsoft.Json;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;

namespace DataApiSFTP
{
	public class DataApiSFTPFileSystem: DataApi, IDisposable
	{
		private const string DefaultDataStoreFtpRelativeRoot = "/projects/VirtualProductions/";
		private const string DefaultDataStoreIngestFolderName = "RawFootage";

		private static readonly Guid LocalStorageGuid = Guid.Parse("00c0ff1e-1329-4357-82ec-000000000001");
		private static readonly DataEntityLocalStorage[] LocalStorages = new[]
		{
			new DataEntityLocalStorage()
			{
				EntityId = LocalStorageGuid,
				LocalStorageName = "CradleNas",
				StorageRoot = new Uri("sftp://cradlenas/projects/VirtualProductions/"),
				BrowsableLocalStorageRoot = new Uri("file://cradlenas/Virtual Productions/")
			}
		};
				
		private static readonly DataEntityPublishedFileType[] PublishedFileTypes = new[]
		{
			new DataEntityPublishedFileType{ EntityId = Guid.Parse("00c0ff1e-1329-4357-82ed-000000000001"), FileType = "video"},
			new DataEntityPublishedFileType{ EntityId = Guid.Parse("00c0ff1e-1329-4357-82ed-000000000002"), FileType = "audio"},
			new DataEntityPublishedFileType{ EntityId = Guid.Parse("00c0ff1e-1329-4357-82ed-000000000003"), FileType = "motion-data"},
		};

		private const string ProjectMetaFileName = "IngestinatorDataApiMeta.json";
		private const string ShotMetaFileName = "IngestinatorShotMeta.json";
		private const string ShotVersionMetaFileName = "IngestinatorShotVersionMeta.json";
		private const string PublishFileMetaFileNameSuffix = ".IngestinatorMeta.json";
		private const string ProjectPublishOverviewFileName = "FilePublishTable.csv";

		private SftpClient? m_client = null;

		private DataApiSFTPConfig m_config;

		public DataApiSFTPFileSystem(DataApiSFTPConfig a_config)
		{
			m_config = a_config;

			foreach(DataEntityLocalStorage storage in LocalStorages)
			{
				LocalCache.AddCachedEntity(storage);
			}
		}

		public override Task<bool> StartConnect()
		{
			if (m_config.SFTPKeyFile == null)
			{
				Logger.LogError("SFTPApi", "Key file not valid for SFTP connection");
				return Task.FromResult(false);
			}

			return Task.Run(() => Connect(m_config.TargetHost, m_config.SFTPUserName, m_config.SFTPKeyFile));
		}

		public bool Connect(string a_hostName, string a_userName, PrivateKeyFile a_keyFile)
		{
			if (m_client == null || !m_client.IsConnected)
			{
				m_client = new SftpClient(a_hostName, 22, a_userName, a_keyFile);
				try
				{
					m_client.Connect();
				}
				catch (SshAuthenticationException ex)
				{
					Logger.LogWarning("SFTPApi", $"SFTP Credentials were rejected at host \"{a_hostName}\": {ex.Message}");
				}
				catch (SocketException ex)
				{
					Logger.LogWarning("SFTPApi", $"Failed to connect with SFTP at host \"{a_hostName}\": {ex.Message}");
				}
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
							attrib = ReadProjectData(m_client, metaPath, directoryEntry.Name);
						}

						if (attrib == null)
						{
							attrib = new DataApiSFTPProjectAttributes();
							string metaAsString = JsonConvert.SerializeObject(attrib, Formatting.Indented);
							m_client.TruncateWriteAllText(metaPath, metaAsString);
						}

						if (attrib.Active)
						{
							DataEntityProject project = attrib.ToDataEntity();
							project.Name = directoryEntry.Name;
							if (project.DataStore == null)
							{
								project.DataStore = new DataEntityReference(LocalStorages[0]);	
							}

							project.ChangeTracker.ClearChangedState();

							LocalCache.AddCachedEntity(project);
							projects.Add(project);
						}
					}
				}

				projects.Sort((a_lhs, a_rhs) => string.Compare(a_lhs.Name, a_rhs.Name, StringComparison.Ordinal));

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

				DataEntityShotVersion? shotVersion = LocalCache.FindEntityById<DataEntityShotVersion>(a_shotVersionId);
				if (shotVersion == null)
				{
					return new DataApiResponse<DataEntityFilePublish>(null, new DataApiErrorDetails($"ShotVersion with Id {a_shotVersionId} was not found in the local cache"));
				}

				if (a_publishData.StorageRoot == null || a_publishData.Path == null)
				{
					return new DataApiResponse<DataEntityFilePublish>(null, new DataApiErrorDetails($"Could not create file publish. Storage root is {(a_publishData.StorageRoot == null? "INVALID" : "valid")}. Path is {(a_publishData.Path == null? "INVALID" : "valid")}"));
				}

				DataEntityLocalStorage? storageRoot = LocalCache.FindEntityById<DataEntityLocalStorage>(a_publishData.StorageRoot.EntityId);
				if (storageRoot == null || storageRoot.StorageRoot == null)
				{
					return new DataApiResponse<DataEntityFilePublish>(null, new DataApiErrorDetails($"Could not create file publish. Storage root with id {a_publishData.StorageRoot.EntityId} is not known by the local cache"));
				}

				if (string.IsNullOrEmpty(a_publishData.RelativePathToStorageRoot))
				{
					Uri relativeUri = storageRoot.StorageRoot.MakeRelativeUri(a_publishData.Path.UriPath!);
					if (relativeUri.OriginalString.Length > 0)
					{
						a_publishData.RelativePathToStorageRoot = relativeUri.OriginalString;
					}
					else
					{
						return new DataApiResponse<DataEntityFilePublish>(null, new DataApiErrorDetails($"File publish was created with invalid relative path: \"{a_publishData.RelativePathToStorageRoot}\""));
					}
				}

				DataApiSFTPFilePublishAttributes attrib = new DataApiSFTPFilePublishAttributes(a_publishData, LocalCache)
				{
					EntityId = Guid.NewGuid()
				};

				string outputFolderPath = GetShotVersionIngestFolderPath(project, shot, shotVersion).ToString();
				string metaAsString = JsonConvert.SerializeObject(attrib, Formatting.Indented);
				m_client.TruncateWriteAllText(outputFolderPath + "/" + a_publishData.PublishedFileName + PublishFileMetaFileNameSuffix, metaAsString);

				DataEntityFilePublish publish = attrib.ToDataEntity(project, shotVersion, LocalCache);
				LocalCache.AddCachedEntity(publish);

				AddFilePublishToOverviewFile(project, shot, shotVersion, publish);

				return new DataApiResponse<DataEntityFilePublish>(publish, null);
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
				m_client.TruncateWriteAllText(outputFolderPath + "/" + ShotMetaFileName, metaAsString);

				DataEntityShot shot = attrib.ToDataEntity(project);
				LocalCache.AddCachedEntity(shot);
				return new DataApiResponse<DataEntityShot>(shot, null);
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
				m_client.TruncateWriteAllText(shotVersionPath + "/" + ShotVersionMetaFileName, metaAsString);

				DataEntityShotVersion version = attribs.ToDataEntity(project, shot);
				LocalCache.AddCachedEntity(version);
				return new DataApiResponse<DataEntityShotVersion>(version, null);
			});
		}

		public override Task<DataApiResponse<DataEntityLocalStorage[]>> GetLocalStorages()
		{
			foreach (DataEntityLocalStorage storage in LocalStorages)
			{
				LocalCache.AddCachedEntity(storage);
			}

			return Task.FromResult(new DataApiResponse<DataEntityLocalStorage[]>(LocalStorages, null));
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
								attrib = ReadShotData(m_client, metaPath, file.Name);
							}

							if (attrib == null)
							{
								attrib = new DataApiSFTPShotAttributes(file.Name);
								string metaAsString = JsonConvert.SerializeObject(attrib, Formatting.Indented);
								m_client.TruncateWriteAllText(metaPath, metaAsString);
							}

							DataEntityShot shot = attrib.ToDataEntity(project);

							shot.ChangeTracker.ClearChangedState();

							LocalCache.AddCachedEntity(shot);
							shots.Add(shot);
						}
					}
				}

				shots.Sort((a_lhs, a_rhs) => string.Compare(a_lhs.ShotName, a_rhs.ShotName, StringComparison.Ordinal));
				return new DataApiResponse<DataEntityShot[]>(shots.ToArray(), null);
			});
		}

		public DataEntityPublishedFileType GetPublishedFileTypeByTag(string a_fileTypeTag)
		{
			DataEntityPublishedFileType[]? fileTypes = GetPublishedFileTypes().Result.ResultData;
			if (fileTypes == null)
			{
				throw new Exception("File types returned null array.");
			}

			DataEntityPublishedFileType? targetFileType = Array.Find(fileTypes, (a_ent) => string.Equals(a_ent.FileType, a_fileTypeTag, StringComparison.InvariantCultureIgnoreCase));
			if (targetFileType == null)
			{
				throw new Exception($"Could not find target file type with tag \"{a_fileTypeTag}\"");
			}

			return targetFileType;
		}

		public override Task<DataApiResponse<DataEntityPublishedFileType[]>> GetPublishedFileTypes()
		{
			foreach (DataEntityPublishedFileType fileType in PublishedFileTypes)
			{
				LocalCache.AddCachedEntity(fileType);
			}

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
								m_client.TruncateWriteAllText(metaPath, metaAsString);
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

				versions.Sort((a_lhs, a_rhs) => string.Compare(a_lhs.ShotVersionName, a_rhs.ShotVersionName, StringComparison.Ordinal));
				return new DataApiResponse<DataEntityShotVersion[]>(versions.ToArray(), null);
			});
		}

		public override Task<DataApiResponse<Uri>> GetBrowsableLocalStoragePathForProject(Guid a_projectId)
		{
			DataEntityProject? project = LocalCache.FindEntityById<DataEntityProject>(a_projectId);
			if (project == null)
			{
				return Task.FromResult(new DataApiResponse<Uri>(null, new DataApiErrorDetails($"Could not find project with ID {a_projectId} in local cache")));
			}

			if (project.DataStore == null)
			{
				return Task.FromResult(new DataApiResponse<Uri>(null, new DataApiErrorDetails($"Project with ID {a_projectId} has no data store attached")));
			}

			DataEntityLocalStorage? localStorage = LocalCache.FindEntityById<DataEntityLocalStorage>(project.DataStore.EntityId);
			if (localStorage == null)
			{
				return Task.FromResult(new DataApiResponse<Uri>(null, new DataApiErrorDetails($"Could not find data store with id {project.DataStore.EntityId} for project {a_projectId}")));
			}

			if (localStorage.BrowsableLocalStorageRoot == null)
			{
				return Task.FromResult(new DataApiResponse<Uri>(null, new DataApiErrorDetails($"Local store with id {project.DataStore.EntityId} does not have a browsable data store path")));
			}

			Uri result = new Uri(localStorage.BrowsableLocalStorageRoot, project.Name);
			return Task.FromResult(new DataApiResponse<Uri>(result, null));
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

			attrib.ProjectName = a_dataEntityProject.Name;

			DataEntityProject project = attrib.ToDataEntity();
			foreach(KeyValuePair<PropertyInfo, object?> kvp in a_changedValues)
			{
				kvp.Key.SetValue(project, kvp.Value);
			}

			attrib = new DataApiSFTPProjectAttributes(project);
			a_client.TruncateWriteAllText(metaPath, JsonConvert.SerializeObject(attrib, Formatting.Indented));

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
			a_client.TruncateWriteAllText(metaPath, JsonConvert.SerializeObject(attrib, Formatting.Indented));

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

			DataApiSFTPShotVersionAttributes? attrib = ReadShotVersionData(a_client, metaPath, a_shotVersionEntity.ShotVersionName);
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
			a_client.TruncateWriteAllText(metaPath, JsonConvert.SerializeObject(attrib, Formatting.Indented));

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

		private DataApiSFTPProjectAttributes? ReadProjectData(SftpClient a_client, string a_metaPath, string a_directoryEntryName)
		{
			DataApiSFTPProjectAttributes? attrib = JsonConvert.DeserializeObject<DataApiSFTPProjectAttributes>(a_client.ReadAllText(a_metaPath));
			if (attrib != null)
			{
				attrib.ProjectName = a_directoryEntryName;
			}

			return attrib;
		}


		private DataApiSFTPShotAttributes? ReadShotData(SftpClient a_client, string a_metaPath, string a_fileName)
		{
			string metaAsString = a_client.ReadAllText(a_metaPath);

			DataApiSFTPShotAttributes? attrib = null;
			try
			{
				attrib = JsonConvert.DeserializeObject<DataApiSFTPShotAttributes>(metaAsString);
			}
			catch (JsonReaderException e)
			{
				Logger.LogError("SFTPDataApi", $"Failed to deserialize file at \"{a_metaPath}\" due to exception: {e.Message}");
			}

			if (attrib != null)
			{
				attrib.ShotName = a_fileName;
			}

			return attrib;
		}

		private DataApiSFTPShotVersionAttributes? ReadShotVersionData(SftpClient a_client, string a_fileLocation, string a_shotVersionName)
		{
			DataApiSFTPShotVersionAttributes? attrib = JsonConvert.DeserializeObject<DataApiSFTPShotVersionAttributes>(a_client.ReadAllText(a_fileLocation));
			if (attrib != null)
			{
				attrib.ShotVersionName = a_shotVersionName;
			}

			return attrib;
		}

		internal class FilePublishEntry
		{
			public string ProjectName { get; set; } = "";
			public string ShotName { get; set; } = "";
			public string VersionName { get; set; } = "";
			public bool GoodTake { get; set; } = false;
			public string FileType { get; set; } = "";
			public string Path { get; set; } = "";
		}

		private void AddFilePublishToOverviewFile(DataEntityProject a_project, DataEntityShot a_shot, DataEntityShotVersion a_shotVersion, DataEntityFilePublish a_publish)
		{
			if (m_client == null)
			{
				throw new Exception("Client is not connected (yet)");
			}

			string targetFile = GetProjectFolderPath(a_project).Append(ProjectPublishOverviewFileName).ToString();

			DataEntityLocalStorage? localStorage = null;
			if (a_publish.StorageRoot != null)
			{
				localStorage = LocalCache.FindEntityById<DataEntityLocalStorage>(a_publish.StorageRoot.EntityId);
			}

			DataEntityPublishedFileType? fileType = null;
			if (a_publish.PublishedFileType != null)
			{
				fileType = LocalCache.FindEntityById<DataEntityPublishedFileType>(a_publish.PublishedFileType.EntityId);
			}

			Uri fullPublishedPath = localStorage?.StorageRoot ?? new Uri(Uri.UriSchemeFile);
			fullPublishedPath = new Uri(fullPublishedPath, a_publish.RelativePathToStorageRoot);

			using (SftpFileStream fs = m_client.Open(targetFile, FileMode.OpenOrCreate, FileAccess.ReadWrite))
			{
				List<FilePublishEntry> entries;
				using (TextReader textReader = new StreamReader(fs, Encoding.UTF8, true, -1, true))
				{
					CsvConfiguration readConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
					{
						MissingFieldFound = null,
						HeaderValidated = OnCsvHeadersValidated
					};
					using (CsvReader reader = new CsvReader(textReader, readConfig, true))
					{
						entries = new List<FilePublishEntry>(reader.GetRecords<FilePublishEntry>());

						entries.Add(new FilePublishEntry
						{
							ProjectName = a_project.Name,
							ShotName = a_shot.ShotName,
							VersionName = a_shotVersion.ShotVersionName,
							GoodTake = a_shotVersion.Flagged,
							FileType = fileType?.FileType ?? "",
							Path = fullPublishedPath.AbsoluteUri,
						});

						entries.Sort((a_lhs, a_rhs) => { 
							int cmp = string.CompareOrdinal(a_lhs.ProjectName, a_rhs.ProjectName);
							if (cmp != 0)
							{
								return cmp;
							}

							cmp = string.CompareOrdinal(a_lhs.ShotName, a_rhs.ShotName);
							if (cmp != 0)
							{
								return cmp;
							}

							return string.CompareOrdinal(a_lhs.VersionName, a_rhs.VersionName);
						});
					}
				}

				fs.SetLength(0);
				fs.Seek(0, SeekOrigin.Begin);

				using (TextWriter textWriter = new StreamWriter(fs, Encoding.UTF8))
				{
					using (CsvWriter writer = new CsvWriter(textWriter, CultureInfo.InvariantCulture, true))
					{
						writer.WriteRecords(entries);
					}
				}
			}
		}

		void OnCsvHeadersValidated(HeaderValidatedArgs args)
		{
			if (args.InvalidHeaders.Count() == 0)
			{
				return;
			}

			var errorMessage = new StringBuilder();
			foreach (var invalidHeader in args.InvalidHeaders)
			{
				errorMessage.AppendLine($"Header with name '{string.Join("' or '", invalidHeader.Names)}'[{invalidHeader.Index}] was not found.");
			}

			if (args.Context.Reader.HeaderRecord != null)
			{
				foreach (var header in args.Context.Reader.HeaderRecord)
				{
					errorMessage.AppendLine($"Headers: '{string.Join("', '", args.Context.Reader.HeaderRecord)}'");
				}
			}

			var messagePostfix =
				$"If you are expecting some headers to be missing and want to ignore this validation, " +
				$"set the configuration {nameof(HeaderValidated)} to null. You can also change the " +
				$"functionality to do something else, like logging the issue.";
			errorMessage.AppendLine(messagePostfix);
		}
	}
}