using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using CommonLogging;
using DataApiCommon;
using DataApiSFTP;
using DataWranglerCommon.IngestDataSources;
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

		private DataApi m_targetApi;
		private DataApiDataRequester m_metaApiDataRequester;
		private Task? m_cacheUpdateTask = null;
		private DataImportWorker m_importWorker;
		private IngestDataCache m_ingestCache = new IngestDataCache();
		private IngestFileReport m_ingestReport = new IngestFileReport();

		private USBDriveEventWatcher m_driveEventWatcher = new USBDriveEventWatcher();
		private CopyProgressWindow m_copyProgress;
		private IngestReportWindow m_ingestReportWindow;

		private readonly IngestDataSourceResolverCollection m_resolverCollection = new IngestDataSourceResolverCollection();

		private readonly List<string> m_importWorkerBacklog = new List<string>();

		public App()
		{
			m_targetApi = new DataApiSFTPFileSystem(DataApiSFTPConfig.DefaultConfig);
			m_metaApiDataRequester = new DataApiDataRequester(m_targetApi);
			m_importWorker = new DataImportWorker(m_targetApi);
			m_importWorker.Start();

			m_importWorker.OnCopyStarted += OnFileCopyStarted;
			m_importWorker.OnCopyUpdate += OnFileCopyUpdate;
			m_importWorker.OnCopyFinished += OnFileCopyFinished;

			m_copyProgress = new CopyProgressWindow();
			m_ingestReportWindow = new IngestReportWindow(m_ingestReport);

			m_driveEventWatcher.OnVolumeChanged += OnVolumeChanged;
		}

		private void OnFileCopyStarted(DataEntityShotVersion a_shotVersion, FileCopyMetaData a_copyMetaData)
		{
			m_copyProgress.SetTargetFile(a_copyMetaData.SourceFilePath.LocalPath, a_copyMetaData.DestinationFullFilePath.LocalPath);
			Dispatcher.InvokeAsync(() => {
				if (!m_ingestReportWindow.IsVisible)
				{
					m_copyProgress.Show();
				}
			});
		}

		private void OnFileCopyUpdate(DataEntityShotVersion a_shotVersion, FileCopyMetaData a_copyMetaData, FileCopyProgress a_progressUpdate)
		{
			string humanReadableCopiedAmount = FormatAsHumanReadableByteAmount(a_progressUpdate.TotalBytesCopied);
			string humanReadableSourceSize = FormatAsHumanReadableByteAmount(a_progressUpdate.TotalFileSizeBytes);
			string humanReadableCopySpeed = FormatAsHumanReadableByteAmount(a_progressUpdate.CurrentCopySpeedBytesPerSecond);

			m_copyProgress.ProgressUpdate(a_progressUpdate.PercentageCopied, $"{humanReadableCopiedAmount} / {humanReadableSourceSize} @ {humanReadableCopySpeed}/s");
			m_copyProgress.UpdateQueueLength(m_importWorker.ImportQueueLength);
		}

		private static string FormatAsHumanReadableByteAmount(long a_byteAmount)
		{
			string[] byteOrderString = new[]
			{
				"B", "KB", "MB", "GB", "TB", "PB"
			};
			int speedUnitOrder = 0;
			double roundedByteAmount = a_byteAmount;
			while (roundedByteAmount > 1024)
			{
				roundedByteAmount /= 1024;
				++speedUnitOrder;
			}

			return $"{roundedByteAmount:0.00} {byteOrderString[speedUnitOrder]}"; ;
		}

		private void OnFileCopyFinished(DataEntityShotVersion a_shotVersion, FileCopyMetaData a_copyMetaData, DataImportWorker.ECopyResult a_copyOperationResult)	
		{
			if (a_copyOperationResult == DataImportWorker.ECopyResult.Success)
			{
				CreatePublishEntryForFile(a_shotVersion, a_copyMetaData);
			}

			m_ingestReport.AddCopiedFile(a_shotVersion, a_copyMetaData, a_copyOperationResult);

			Dispatcher.InvokeAsync(() => {
				if (m_importWorker.ImportQueueLength == 0)
				{
					m_copyProgress.Hide();
					m_ingestReportWindow.Show();
				}
			});
		}

		private void CreatePublishEntryForFile(DataEntityShotVersion a_shotVersion, FileCopyMetaData a_copyMetaData)
		{
			if (a_shotVersion.EntityRelationships.Project == null)
			{
				Logger.LogError("DataImporter", $"Failed to publish entry for take {a_shotVersion.EntityId}. Project relationship was not properly set");
				return;
			}

			if (a_shotVersion.EntityRelationships.Parent == null)
			{
				Logger.LogError("DataImporter", $"Failed to publish entry for take {a_shotVersion.EntityId}. Parent entity relationship to the Shot was not properly set");
				return;
			}

			string publishFileName = Path.GetFileName(a_copyMetaData.SourceFilePath.LocalPath);

			DataEntityFilePublish publishData = new DataEntityFilePublish
			{
				Path = new DataEntityFileLink(a_copyMetaData.DestinationFullFilePath),
				StorageRoot = new DataEntityReference(a_copyMetaData.StorageTarget),
				PublishedFileName = publishFileName,
				PublishedFileType = new DataEntityReference(a_copyMetaData.FileTag),
				//Status = ServiceWorkerConfig.Instance.FilePublishDefaultStatus,
				Description = ServiceWorkerConfig.Instance.FilePublishDescription
			};

			m_targetApi.CreateFilePublish(a_shotVersion.EntityRelationships.Project.EntityId, a_shotVersion.EntityRelationships.Parent.EntityId, a_shotVersion.EntityId, publishData)
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

			APIConnectionWindow window = new APIConnectionWindow(m_targetApi);
			window.OnSuccessfulConnect += OnSuccessfulLogin;
		}
		
		private void OnSuccessfulLogin()
		{
			//m_targetApi.StartAutoRefreshToken(OnRequestUserAuthentication);

			m_cacheUpdateTask = m_metaApiDataRequester.RequestAllRelevantData();
			m_cacheUpdateTask.ContinueWith(_ =>
			{
				m_ingestCache.UpdateCache(m_targetApi.LocalCache);
				foreach (string rootPath in m_importWorkerBacklog)
				{
					new FileMetaResolverWorker(rootPath, m_targetApi.LocalCache, m_importWorker, m_resolverCollection, m_ingestCache, m_ingestReport).Run();
				}

				TryImportFilesFromMeta();
			});

			m_driveEventWatcher.DetectCurrentlyPresentUSBDrives();

			/*
			DataEntityLocalStorage testStorage = new DataEntityLocalStorage() {StorageRoot = new Uri("D:/")};

			IngestFileReport testReport = new IngestFileReport();
			IngestFileResolutionDetails testDetails = new IngestFileResolutionDetails("C:/success");
			testDetails.AddRejection(new IngestShotVersionIdentifier(new DataEntityShotVersion {EntityId = Guid.NewGuid(), ShotVersionName = "test_version_01"}, null), "Some random rejection");
			testDetails.AddRejection(new IngestShotVersionIdentifier(new DataEntityShotVersion {EntityId = Guid.NewGuid(), ShotVersionName = "test_version_02"}, null), "Computer said no");
			testReport.AddFileResolutionDetails(testDetails);
			testReport.AddCopiedFile(new DataEntityShotVersion { }, new FileCopyMetaData("C:/success", "D:/success", testStorage, new DataEntityPublishedFileType()), DataImportWorker.ECopyResult.Success);
			testReport.AddCopiedFile(new DataEntityShotVersion { }, new FileCopyMetaData("C:/up_to_date", "D:/up_to_date", testStorage, new DataEntityPublishedFileType()), DataImportWorker.ECopyResult.FileAlreadyUpToDate);
			testReport.AddCopiedFile(new DataEntityShotVersion { }, new FileCopyMetaData("C:/invalid_destination", "D:/invalid_destination", testStorage, new DataEntityPublishedFileType()), DataImportWorker.ECopyResult.InvalidDestinationPath);
			testReport.AddCopiedFile(new DataEntityShotVersion { }, new FileCopyMetaData("C:/unknown_fail", "D:/unknown_fail", testStorage, new DataEntityPublishedFileType()), DataImportWorker.ECopyResult.UnknownFailure);
			(new IngestReportWindow(testReport)).Show();
			*/
		}

		private void OnLoggerMessageLogged(string a_source, ELogSeverity a_severity, string a_message)
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
					new FileMetaResolverWorker(a_e.DriveRootPath, m_targetApi.LocalCache, m_importWorker, m_resolverCollection, m_ingestCache, m_ingestReport).Run();
				}
			}
		}

		private void TryImportFilesFromMeta()
		{
			foreach (IngestDataSourceResolver metaResolver in m_resolverCollection.DataSourceResolvers)
			{
				if (metaResolver.CanProcessCache)
				{
					List<IngestDataSourceResolver.IngestFileEntry> filesToIngest = metaResolver.ProcessCache(m_targetApi.LocalCache, m_ingestCache);
					foreach (IngestDataSourceResolver.IngestFileEntry entry in filesToIngest)
					{
						m_importWorker.AddFileToImport(entry.TargetShotVersion, entry.SourcePath, entry.FileTag);
					}
				}
			}
		}
	}
}
