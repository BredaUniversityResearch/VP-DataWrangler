using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using DataWranglerCommon;
using ShotGridIntegration;

namespace DataWranglerServiceWorker
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		[DllImport("Kernel32")]
		public static extern void AllocConsole();

		[DllImport("Kernel32")]
		public static extern void FreeConsole();

		private ShotGridAPI m_targetApi;
		private ShotGridDataCache m_metaCache;
		private Task? m_cacheUpdateTask = null;
		private DataImportWorker m_importWorker;

		private USBDriveEventWatcher m_driveEventWatcher = new USBDriveEventWatcher();
		private CopyProgressWindow m_copyProgress;

		private List<string> m_importWorkerBacklog = new List<string>();


		public App()
		{
			m_targetApi = new ShotGridAPI();
			m_metaCache = new ShotGridDataCache(m_targetApi);
			m_importWorker = new DataImportWorker(m_metaCache, m_targetApi);
			m_importWorker.Start();

			m_importWorker.OnCopyStarted += OnFileCopyStarted;
			m_importWorker.OnCopyUpdate += OnFileCopyUpdate;
			m_importWorker.OnCopyFinished += OnFileCopyFinished;

			m_copyProgress = new CopyProgressWindow();

			m_driveEventWatcher.OnVolumeChanged += OnVolumeChanged;
		}

		private void OnFileCopyStarted(ShotVersionIdentifier a_shotVersion, FileCopyMetaData a_copyMetaData)
		{
			m_copyProgress.SetTargetFile(a_copyMetaData.SourceFilePath, a_copyMetaData.DestinationFullFilePath);
			Dispatcher.InvokeAsync(() => {
				if (!m_copyProgress.IsVisible)
				{
					m_copyProgress.Show();
				}
			});
		}

		private void OnFileCopyUpdate(ShotVersionIdentifier a_shotVersion, FileCopyMetaData a_copyMetaData, float a_progressPercent)
		{
			m_copyProgress.ProgressUpdate(a_progressPercent);
			m_copyProgress.UpdateQueueLength(m_importWorker.ImportQueueLength);
		}

		private void OnFileCopyFinished(ShotVersionIdentifier a_shotVersion, FileCopyMetaData a_copyMetaData, DataImportWorker.ECopyResult a_copyOperationResult)	
		{
			Dispatcher.InvokeAsync(() => {
				if (m_importWorker.ImportQueueLength == 0)
				{
					m_copyProgress.Hide();
				}
			});

			//if (a_copyOperationResult == DataImportWorker.ECopyResult.Success)
			{
				CreatePublishEntryForFile(a_shotVersion, a_copyMetaData, "video");
			}
		}

		private void CreatePublishEntryForFile(ShotVersionIdentifier a_shotVersion, FileCopyMetaData a_copyMetaData, string a_fileTypeRelation)
		{
			if (!m_metaCache.FindEntityById(a_shotVersion.ShotId, out ShotGridEntityShot? shotData))
			{
				Logger.LogError("DataImporter", $"Failed to get shot data for shot id {a_shotVersion.ShotId}. File won't be marked as published in shotgrid");
				return;
			}

			if (!m_metaCache.FindEntityById(a_shotVersion.VersionId, out ShotGridEntityShotVersion? versionData))
			{
				Logger.LogError("DataImporter", $"Failed to get shot version data for shot id {a_shotVersion.VersionId}. File won't be marked as published in shotgrid");
				return;
			}

			ShotGridEntityReference? fileTypeTagReference = null;
			ShotGridAPIResponse<ShotGridEntityRelation?> fileTypeRelation = m_targetApi.FindRelationByCode(ShotGridEntity.TypeNames.PublishedFileType, "video").Result;
			if (!fileTypeRelation.IsError)
			{
				fileTypeTagReference = ShotGridEntityReference.Create(ShotGridEntity.TypeNames.PublishedFileType, fileTypeRelation.ResultData);
			}
			else
			{
				Logger.LogError("DataImporter", $"Failed to find file type relation {a_fileTypeRelation} in shotgrid. Entity won't be linked");
			}

			string publishFileName = $"{shotData.Attributes.ShotCode}_{versionData.Attributes.VersionCode}";
			ShotGridEntityFilePublish.FilePublishAttributes publishData = new ShotGridEntityFilePublish.FilePublishAttributes
			{
				Path = new ShotGridEntityFilePublish.FileLink
				{
					FileName = publishFileName,
					LinkType = "local",
					LocalPath = a_copyMetaData.DestinationFullFilePath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
					LocalStorageTarget = new ShotGridEntityReference("LocalStorage", 1),
					//LocalPathLinux = a_copyMetaData.DestinationDataStoreRoot + a_copyMetaData.DestinationRelativeFilePath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
					//LocalPathMac = a_copyMetaData.DestinationDataStoreRoot + a_copyMetaData.DestinationRelativeFilePath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
					//LocalPathWindows = a_copyMetaData.DestinationFullFilePath,
					Url = new UriBuilder { Scheme = Uri.UriSchemeFile, Path = a_copyMetaData.DestinationFullFilePath }.Uri.AbsoluteUri
				},
				//PathCache = a_copyMetaData.DestinationRelativeFilePath,
				//PathCacheStorage = ShotGridEntityReference.Create(a_copyMetaData.StorageTarget),
				PublishedFileName = publishFileName,
				PublishedFileType = fileTypeTagReference,
				Description = "File auto-published by Data Wrangler"
			};

			

			m_targetApi.CreateFilePublish(a_shotVersion.ProjectId, a_shotVersion.ShotId, a_shotVersion.VersionId, publishData)
				.ContinueWith(a_taskResult =>
				{
					if (!a_taskResult.Result.IsError)
					{
						Logger.LogInfo("DataImporter", $"Successfully published file {a_copyMetaData.DestinationFullFilePath}");
					}
					else
					{
						Logger.LogError("DataImporter", $"Failed to publish file {a_copyMetaData.DestinationFullFilePath}. Error: {a_taskResult.Result.ErrorInfo}");
					}
				});
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			AllocConsole();
			Logger.Instance.OnMessageLogged += OnLoggerMessageLogged;

			Console.WriteLine("DataWranglerServiceWorker");

			ShotGridAuthenticationWindow window = new ShotGridAuthenticationWindow(m_targetApi);
			window.EnsureLogin();
			window.OnSuccessfulLogin += OnSuccessfulLogin;
		}

		private void OnSuccessfulLogin()
		{
			m_cacheUpdateTask = m_metaCache.UpdateCache();
			m_cacheUpdateTask.ContinueWith(_ =>
				{
					foreach (string rootPath in m_importWorkerBacklog)
					{
						new FileDiscoveryWorker(rootPath, m_metaCache, m_importWorker).Run();
					}
				});

			//t.ContinueWith((a_task) =>
			//{
			//	m_importWorker.Start();
			//	new FileDiscoveryWorker("E:\\", m_metaCache, m_importWorker).Run();
			//});

			m_cacheUpdateTask.ContinueWith((a_task) => { 
				CreatePublishEntryForFile(ShotVersionIdentifier
			});
		}

		private void OnLoggerMessageLogged(string a_source, string a_severity, string a_message)
		{
			Console.WriteLine($"{a_source}\t{a_severity}\t{a_message}");
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			m_copyProgress.Close();

			FreeConsole();
		}

		private void OnVolumeChanged(USBDriveEventWatcher.VolumeChangedEvent a_e)
		{
			if (a_e.EventType == USBDriveEventWatcher.VolumeChangedEvent.EEventType.DeviceArrival)
			{
				if (m_cacheUpdateTask != null && !m_cacheUpdateTask.IsCompleted)
				{
					m_importWorkerBacklog.Add(a_e.DriveRootPath);
				}
				else
				{
					new FileDiscoveryWorker(a_e.DriveRootPath, m_metaCache, m_importWorker).Run();
				}
			}
		}
	}
}
