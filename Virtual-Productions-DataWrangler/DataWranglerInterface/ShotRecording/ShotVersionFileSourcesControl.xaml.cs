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
		private List<UserControl> m_currentFileSourceControls = new List<UserControl>();

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
			m_currentFileSourceControls.Clear();

			Dispatcher.InvokeAsync(() =>
			{
				FileSourceControl.Children.Clear();
				foreach (IngestDataSourceMeta fs in m_currentMeta.FileSources)
				{
					AddMetaEditor(DataWranglerFileSourceUIDecorator.CreateEditorForMeta(fs), fs);
				}
			});
		}

		private void AddMetaEditor(UserControl a_metaEditorControl, IngestDataSourceMeta a_editingMeta)
		{
			m_currentFileSourceControls.Add(a_metaEditorControl);
			FileSourceControl.Children.Add(new DataWranglerFileSourceUIDecorator(a_metaEditorControl, null));
		}
	}
}
