using System.Windows;
using System.Windows.Controls;
using DataWranglerCommon;
using DataWranglerCommon.IngestDataSources;

namespace DataWranglerInterface.ShotRecording
{
    /// <summary>
    /// Interaction logic for ShotVersionTemplateFileSourcesControl.xaml
    /// </summary>
    public partial class ShotVersionFileSourcesControl : UserControl
	{
		private IngestDataShotVersionMeta m_currentMeta = new IngestDataShotVersionMeta();

		public ShotVersionFileSourcesControl()
		{
			InitializeComponent();
		}

		public void SetCurrentMeta(IngestDataShotVersionMeta a_meta)
		{
			m_currentMeta = a_meta;
			UpdateDisplayedWidgets();
		}

		public void UpdateDisplayedWidgets()
		{
			Dispatcher.InvokeAsync(() =>
			{
				FileSourceControl.Children.Clear();
				foreach (IngestDataSourceMeta fs in m_currentMeta.FileSources)
				{
					FileSourceControl.Children.Add(new DataWranglerFileSourceUIDecorator(fs, null));
				}
			});
		}
	}
}
