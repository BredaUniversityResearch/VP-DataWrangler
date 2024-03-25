using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DataWranglerServiceWorker
{
	/// <summary>
	/// Interaction logic for IngestFileReportEntryWindow.xaml
	/// </summary>
	public partial class IngestFileReportEntryWindow : Window
	{
		public IngestFileReportEntry Entry { get; }

		public IngestFileReportEntryWindow(IngestFileReportEntry a_entry)
		{
			Entry = a_entry;

			InitializeComponent();
		}

		private void OpenFileButton_OnClick(object a_sender, RoutedEventArgs a_e)
		{
			Process.Start(new ProcessStartInfo()
				{
					FileName = Entry.SourceFile.LocalPath,
					UseShellExecute = true,
					Verb = "open"
				}
			);
		}

		private void BrowseFileButton_OnClick(object a_sender, RoutedEventArgs a_e)
		{
			string selectArgument = $"/select,\"{Entry.SourceFile.LocalPath}\"";

			Process.Start(new ProcessStartInfo()
				{
					FileName = "explorer.exe",
					UseShellExecute = true,
					Verb = "open", 
					Arguments = selectArgument
				}
			);
		}
	}
}
