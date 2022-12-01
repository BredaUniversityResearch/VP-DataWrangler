using System;
using System.Collections.Generic;
using System.ComponentModel;
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
	/// Interaction logic for CopyProgressWindow.xaml
	/// </summary>
	public partial class CopyProgressWindow : Window
	{
		public CopyProgressWindow()
		{
			InitializeComponent();

			Closing += OnClosingCancelEvent;
		}

		private void OnClosingCancelEvent(object? a_sender, CancelEventArgs a_e)
		{
			a_e.Cancel = true;
		}

		public void SetTargetFile(string a_sourceFile, string a_destinationFile)
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.InvokeAsync(() => { SetTargetFile(a_sourceFile, a_destinationFile); });
				return;
			}

			CurrentFileName.Content = a_sourceFile;
		}

		public void ProgressUpdate(float a_progressPercent, string a_humanReadableSpeed)
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.InvokeAsync(() => { ProgressUpdate(a_progressPercent, a_humanReadableSpeed); });
				return;
			}

			CopyProgressIndicator.Value = a_progressPercent * 100.0f;
			ProgressInformation.Content = a_humanReadableSpeed;
		}

		public void UpdateQueueLength(int a_queueLength)
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.InvokeAsync(() => { UpdateQueueLength(a_queueLength); });
				return;
			}

			QueueLength.Content = $"Files queued for copy: {a_queueLength}";
		}
	}
}
