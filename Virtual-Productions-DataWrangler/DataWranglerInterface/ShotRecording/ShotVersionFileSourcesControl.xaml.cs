using System.Windows;
using System.Windows.Controls;
using DataWranglerCommon;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for ShotVersionFileSourcesControl.xaml
	/// </summary>
	public partial class ShotVersionFileSourcesControl : UserControl
	{
		private DataWranglerShotVersionMeta? m_currentTargetMeta;
		private List<UserControl> m_currentFileSourceControls = new List<UserControl>();

		private ContextMenu m_addFileSourceContextMenu;

		public ShotVersionFileSourcesControl()
		{
			m_addFileSourceContextMenu = new ContextMenu();
			MenuItem item = new MenuItem
			{
				Header = "Blackmagic Ursa (BT)"
			};
			item.Click += OnAddSourceContextMenuClick;
			m_addFileSourceContextMenu.Items.Add(item);

			InitializeComponent();

		}

		public void UpdateData(DataWranglerShotVersionMeta a_currentVersionMeta)
		{
			m_currentFileSourceControls.Clear();

			m_currentTargetMeta = a_currentVersionMeta;

			Dispatcher.InvokeAsync(() =>
			{
				FileSourceControl.Children.Clear();
				foreach (DataWranglerFileSourceMeta fs in m_currentTargetMeta.FileSources)
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
			if (m_currentTargetMeta == null)
			{
				return;
			}

			DataWranglerFileSourceMeta? meta = m_currentTargetMeta.FileSources.Find(source => source.SourceType == DataWranglerFileSourceMetaBlackmagicUrsa.MetaSourceType);
			if (meta == null || !meta.IsUniqueMeta)
			{
				m_currentTargetMeta.FileSources.Add(new DataWranglerFileSourceMetaBlackmagicUrsa());
			}
			else
			{
				Logger.LogWarning("UI", $"Could not add meta of type {meta.SourceType} as this is marked as being a unique meta.");
			}
			UpdateData(m_currentTargetMeta);
		}

		private void RemoveMeta(DataWranglerFileSourceMeta a_meta)
		{
			if (m_currentTargetMeta == null)
			{
				return;
			}

			bool success = m_currentTargetMeta.FileSources.Remove(a_meta);
			if (!success)
			{
				throw new Exception("Failed to remove meta from file sources");
			}

			UpdateData(m_currentTargetMeta);
		}
	}
}
