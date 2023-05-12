using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using CommonLogging;
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

		private readonly IMetaFileResolver[] m_availableMetaFileResolvers = new[]
		{
			new MetaFileResolverVicon()
		};

		private readonly List<string> m_importWorkerBacklog = new List<string>();

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
			m_copyProgress.SetTargetFile(a_copyMetaData.SourceFilePath.LocalPath, a_copyMetaData.DestinationFullFilePath.LocalPath);
			Dispatcher.InvokeAsync(() => {
				if (!m_copyProgress.IsVisible)
				{
					m_copyProgress.Show();
				}
			});
		}

		private void OnFileCopyUpdate(ShotVersionIdentifier a_shotVersion, FileCopyMetaData a_copyMetaData, FileCopyProgress a_progressUpdate)
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

		private void OnFileCopyFinished(ShotVersionIdentifier a_shotVersion, FileCopyMetaData a_copyMetaData, DataImportWorker.ECopyResult a_copyOperationResult)	
		{
			Dispatcher.InvokeAsync(() => {
				if (m_importWorker.ImportQueueLength == 0)
				{
					m_copyProgress.Hide();
				}
			});

			if (a_copyOperationResult == DataImportWorker.ECopyResult.Success)
			{
				CreatePublishEntryForFile(a_shotVersion, a_copyMetaData);
			}
		}

		private void CreatePublishEntryForFile(ShotVersionIdentifier a_shotVersion, FileCopyMetaData a_copyMetaData)
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

			string publishFileName = Path.GetFileName(a_copyMetaData.SourceFilePath.LocalPath);
			ShotGridEntityFilePublish.FilePublishAttributes publishData = new ShotGridEntityFilePublish.FilePublishAttributes
			{
				Path = new ShotGridFileLink(a_copyMetaData.DestinationFullFilePath),
				PublishedFileName = publishFileName,
				PublishedFileType = ShotGridEntityReference.Create(ShotGridEntityName.PublishedFileType, a_copyMetaData.FileTag),
				Status = ServiceWorkerConfig.Instance.FilePublishDefaultStatus,
				Description = ServiceWorkerConfig.Instance.FilePublishDescription
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

			OnRequestUserAuthentication();
		}

		private void OnRequestUserAuthentication()
		{
			ShotGridAuthenticationWindow window = new ShotGridAuthenticationWindow(m_targetApi);
			window.EnsureLogin();
			window.OnSuccessfulLogin += OnSuccessfulLogin;
		}

		private void OnSuccessfulLogin()
		{
			m_targetApi.StartAutoRefreshToken(OnRequestUserAuthentication);

			m_cacheUpdateTask = m_metaCache.UpdateCache();
			m_cacheUpdateTask.ContinueWith(_ =>
				{
					foreach (string rootPath in m_importWorkerBacklog)
					{
						new FileMetaResolverWorker(rootPath, m_metaCache, m_importWorker).Run();
					}

					TryImportFilesFromMeta();
				});

			m_driveEventWatcher.DetectCurrentlyPresentUSBDrives();
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
					new FileMetaResolverWorker(a_e.DriveRootPath, m_metaCache, m_importWorker).Run();
				}
			}
		}

		private void TryImportFilesFromMeta()
		{
			foreach (IMetaFileResolver metaResolver in m_availableMetaFileResolvers)
			{
				metaResolver.ProcessCache(m_metaCache, m_importWorker);
			}
		}
	}
}
