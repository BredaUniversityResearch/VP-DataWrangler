using System;
using System.Collections.Generic;
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
	}
}
