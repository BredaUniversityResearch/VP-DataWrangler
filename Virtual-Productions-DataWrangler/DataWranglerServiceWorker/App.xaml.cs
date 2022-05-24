using System;
using System.Collections.Generic;
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

		private void OnFileCopyStarted(string a_sourceFile, string a_destinationFile)
		{
			m_copyProgress.SetTargetFile(a_sourceFile, a_destinationFile);
			Dispatcher.InvokeAsync(() => {
				if (!m_copyProgress.IsVisible)
				{
					m_copyProgress.Show();
				}
			});
		}

		private void OnFileCopyUpdate(string a_destinationFile, float a_progressPercent)
		{
			m_copyProgress.ProgressUpdate(a_progressPercent);
			m_copyProgress.UpdateQueueLength(m_importWorker.ImportQueueLength);
		}

		private void OnFileCopyFinished(string a_destinationFile)	
		{
			Dispatcher.InvokeAsync(() => {
				if (m_importWorker.ImportQueueLength == 0)
				{
					m_copyProgress.Hide();
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
