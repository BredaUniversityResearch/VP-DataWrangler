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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DataWranglerInterface.DebugSupport
{
	/// <summary>
	/// Interaction logic for LogEntry.xaml
	/// </summary>
	public partial class LogEntry : UserControl
	{
		public string Source {get; }
		public string Severity {get; }
		public string Message { get; }

		public LogEntry(string a_source, string a_severity, string a_message)
		{
			Source = a_source;
			Severity = a_severity;
			Message = a_message;

			InitializeComponent();
		}
	}
}
