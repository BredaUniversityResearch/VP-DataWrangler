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
	}
}
