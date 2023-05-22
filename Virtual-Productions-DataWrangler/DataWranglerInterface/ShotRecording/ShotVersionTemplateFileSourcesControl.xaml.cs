using System.Windows;
using System.Windows.Controls;
using CommonLogging;
using DataWranglerCommon;
using DataWranglerCommon.IngestDataSources;

namespace DataWranglerInterface.ShotRecording
{
    /// <summary>
    /// Interaction logic for ShotVersionTemplateFileSourcesControl.xaml
    /// </summary>
    public partial class ShotVersionTemplateFileSourcesControl : UserControl
	{
		private readonly IngestDataShotVersionMeta m_currentTemplateMeta = new IngestDataShotVersionMeta();
		private List<UserControl> m_currentFileSourceControls = new List<UserControl>();

		private ContextMenu m_addFileSourceContextMenu;

		public ShotVersionTemplateFileSourcesControl()
		{
			m_addFileSourceContextMenu = new ContextMenu();
			//MenuItem item = new MenuItem
			//{
			//	Header = "Blackmagic Ursa"
			//};
			//item.Click += (_, _) => TryAddSource(new DataWranglerFileSourceMetaBlackmagicUrsa());
			//m_addFileSourceContextMenu.Items.Add(item);
			//
			//item = new MenuItem
			//{
			//	Header = "TASCAM DR-60 MkII"
			//};
			//item.Click += (_, _) => TryAddSource(new DataWranglerFileSourceMetaTascam());
			//m_addFileSourceContextMenu.Items.Add(item);
			//
			//item = new MenuItem
			//{
			//	Header = "Vicon Motion Data"
			//};
			//item.Click += (_, _) => TryAddSource(new DataWranglerFileSourceMetaViconTrackingData());
			//m_addFileSourceContextMenu.Items.Add(item);

			InitializeComponent();

			m_currentTemplateMeta.FileSources.Add(new IngestDataSourceMetaBlackmagicUrsa());
			UpdateDisplayedWidgets();
		}

		public IngestDataShotVersionMeta CreateMetaFromCurrentTemplate()
		{
			IngestDataShotVersionMeta meta = m_currentTemplateMeta.Clone();
			return meta;
		}

		public void UpdateDisplayedWidgets()
		{
			m_currentFileSourceControls.Clear();

			Dispatcher.InvokeAsync(() =>
			{
				FileSourceControl.Children.Clear();
				foreach (IngestDataSourceMeta fs in m_currentTemplateMeta.FileSources)
				{
					AddMetaEditor(DataWranglerFileSourceUIDecorator.CreateEditorForMeta(fs), fs);
				}
			});
		}

		private void AddMetaEditor(UserControl a_metaEditorControl, IngestDataSourceMeta a_editingMeta)
		{
			m_currentFileSourceControls.Add(a_metaEditorControl);
			FileSourceControl.Children.Add(new DataWranglerFileSourceUIDecorator(a_metaEditorControl, () => { RemoveMeta(a_editingMeta); }));
		}

		private void AddFileSourceButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			m_addFileSourceContextMenu.PlacementTarget = AddFileSourceButton;
			m_addFileSourceContextMenu.IsOpen = true;
		}

		private void TryAddSource(IngestDataSourceMeta a_meta)
		{
			IngestDataSourceMeta? meta = m_currentTemplateMeta.FileSources.Find(source => source.SourceType == a_meta.SourceType);
			if (meta == null)
			{
				m_currentTemplateMeta.FileSources.Add(a_meta);
				UpdateDisplayedWidgets();
			}
			else
			{
				Logger.LogWarning("UI", $"Could not add meta of type {meta.SourceType} as this is marked as being a unique meta.");
			}
		}

		private void RemoveMeta(IngestDataSourceMeta a_meta)
		{
			bool success = m_currentTemplateMeta.FileSources.Remove(a_meta);
			if (!success)
			{
				throw new Exception("Failed to remove meta from file sources");
			}

			UpdateDisplayedWidgets();
		}
	}
}
