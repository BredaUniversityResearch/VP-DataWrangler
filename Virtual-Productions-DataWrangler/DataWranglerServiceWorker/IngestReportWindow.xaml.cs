using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DataWranglerServiceWorker
{
	/// <summary>
	/// Interaction logic for IngestReportWindow.xaml
	/// </summary>
	public partial class IngestReportWindow : Window
	{
		public IngestFileReport Report { get; private set; }

		public IngestReportWindow(IngestFileReport a_report)
		{
			Report = a_report;

			InitializeComponent();
		}

		private void DataGridRow_MouseDoubleClick(object a_sender, MouseButtonEventArgs a_e)
		{
			DataGridRow row = (DataGridRow) a_sender;
			if (row.Item is IngestFileReportEntry entry)
			{
				(new IngestFileReportEntryWindow(entry)).Show();
			}
		}


		public void SetTargetFile(Uri a_sourceFile, string a_destinationFile)
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.InvokeAsync(() => { SetTargetFile(a_sourceFile, a_destinationFile); });
				return;
			}

			CurrentFileName.Content = a_sourceFile;
			IngestFileReportEntry? entry = Report.FindEntryForFilePath(a_sourceFile);
			if (entry != null)
			{
				entry.Status = "Starting Copy...";
			}
		}

		public void ProgressUpdate(Uri a_sourceFile, float a_progressPercent, string a_humanReadableSpeed)
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.InvokeAsync(() => { ProgressUpdate(a_sourceFile, a_progressPercent, a_humanReadableSpeed); });
				return;
			}

			CopyOperationContainer.Visibility = Visibility.Visible;
			CopyProgressIndicator.Value = a_progressPercent * 100.0f;
			ProgressInformation.Content = a_humanReadableSpeed;

			IngestFileReportEntry? entry = Report.FindEntryForFilePath(a_sourceFile);
			if (entry != null)
			{
				entry.Status = $"Copying {a_progressPercent:P}";
			}
		}

		public void OnAllCopyOperationsFinished()
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.InvokeAsync(OnAllCopyOperationsFinished);
				return;
			}

			CopyOperationContainer.Visibility = Visibility.Collapsed;
		}

		public void OnFileCopyCompleted(Uri a_sourceFilePath, DataImportWorker.ECopyResult a_copyOperationResult)
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.InvokeAsync(() => OnFileCopyCompleted(a_sourceFilePath, a_copyOperationResult));
				return;
			}

			if (a_copyOperationResult == DataImportWorker.ECopyResult.FileAlreadyUpToDate)
			{
				IngestFileReportEntry? entry = Report.FindEntryForFilePath(a_sourceFilePath);
				if (entry != null)
				{
					entry.Status = "File already up to date";
				}
			}
		}

		public void OnFileCopyStartWriteMetaData(Uri a_sourceFilePath)
		{
			IngestFileReportEntry? entry = Report.FindEntryForFilePath(a_sourceFilePath);
			if (entry != null)
			{
				entry.Status = "Writing Metadata...";
			}
		}
	}
}
