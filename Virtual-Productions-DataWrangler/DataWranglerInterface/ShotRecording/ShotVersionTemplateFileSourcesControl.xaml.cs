using System.Windows;
using System.Windows.Controls;
using DataWranglerCommon;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for ShotVersionTemplateFileSourcesControl.xaml
	/// </summary>
	public partial class ShotVersionTemplateFileSourcesControl : UserControl
	{
		private readonly DataWranglerShotVersionMeta m_currentTemplateMeta = new DataWranglerShotVersionMeta();
		private List<UserControl> m_currentFileSourceControls = new List<UserControl>();

		private ContextMenu m_addFileSourceContextMenu;

		public ShotVersionTemplateFileSourcesControl()
		{
			m_addFileSourceContextMenu = new ContextMenu();
			MenuItem item = new MenuItem
			{
				Header = "Blackmagic Ursa (BT)"
			};
			item.Click += OnAddSourceContextMenuClick;
			m_addFileSourceContextMenu.Items.Add(item);

			InitializeComponent();

			m_currentTemplateMeta.FileSources.Add(new DataWranglerFileSourceMetaBlackmagicUrsa());
			UpdateDisplayedWidgets();
		}

		public DataWranglerShotVersionMeta CreateMetaFromCurrentTemplate()
		{
			return m_currentTemplateMeta.Clone();
		}

		public void UpdateDisplayedWidgets()
		{
			m_currentFileSourceControls.Clear();

			Dispatcher.InvokeAsync(() =>
			{
				FileSourceControl.Children.Clear();
				foreach (DataWranglerFileSourceMeta fs in m_currentTemplateMeta.FileSources)
				{
					if (fs is DataWranglerFileSourceMetaBlackmagicUrsa ursaSource)
					{
						AddMetaEditor(new DataWranglerFileSourceUIBlackmagicUrsa(ursaSource), fs);
					}
				}
			});
		}

		private void AddMetaEditor(UserControl a_metaEditorControl, DataWranglerFileSourceMeta a_editingMeta)
		{
			m_currentFileSourceControls.Add(a_metaEditorControl);
			FileSourceControl.Children.Add(new DataWranglerFileSourceUIDecorator(a_metaEditorControl, () => { RemoveMeta(a_editingMeta); }));
		}

		private void AddFileSourceButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			m_addFileSourceContextMenu.PlacementTarget = AddFileSourceButton;
			m_addFileSourceContextMenu.IsOpen = true;
		}

		private void OnAddSourceContextMenuClick(object a_sender, RoutedEventArgs a_e)
		{
			DataWranglerFileSourceMeta? meta = m_currentTemplateMeta.FileSources.Find(source => source.SourceType == DataWranglerFileSourceMetaBlackmagicUrsa.MetaSourceType);
			if (meta == null || !meta.IsUniqueMeta)
			{
				m_currentTemplateMeta.FileSources.Add(new DataWranglerFileSourceMetaBlackmagicUrsa());
				UpdateDisplayedWidgets();
			}
			else
			{
				Logger.LogWarning("UI", $"Could not add meta of type {meta.SourceType} as this is marked as being a unique meta.");
			}
		}

		private void RemoveMeta(DataWranglerFileSourceMeta a_meta)
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
