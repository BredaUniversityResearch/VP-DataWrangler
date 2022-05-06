using System;
using System.Runtime.InteropServices;
using System.Windows;
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

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			AllocConsole();
			Console.WriteLine("DataWranglerServiceWorker");

			ShotGridAPI api = new ShotGridAPI();
			ShotGridAuthenticationWindow window = new ShotGridAuthenticationWindow(api);
			window.EnsureLogin();
		}
	}
}
