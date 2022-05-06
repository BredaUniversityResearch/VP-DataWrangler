using System;
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
		private ShotGridDataWranglerShotVersionMetaCache m_metaCache;

		private USBDriveEventWatcher m_driveEventWatcher = new USBDriveEventWatcher();

		public App()
		{
			m_targetApi = new ShotGridAPI();
			m_metaCache = new ShotGridDataWranglerShotVersionMetaCache(m_targetApi);
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
			Task t = m_metaCache.UpdateCache();
		}

		private void OnLoggerMessageLogged(string a_source, string a_severity, string a_message)
		{
			Console.WriteLine($"{a_source}\t{a_severity}\t{a_message}");
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			FreeConsole();
		}
	}
}
