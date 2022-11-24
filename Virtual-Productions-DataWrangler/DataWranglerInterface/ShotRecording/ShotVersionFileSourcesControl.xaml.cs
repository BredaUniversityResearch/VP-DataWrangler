using System.Windows;
using System.Windows.Controls;
using DataWranglerCommon;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for ShotVersionTemplateFileSourcesControl.xaml
	/// </summary>
	public partial class ShotVersionFileSourcesControl : UserControl
	{
		private DataWranglerShotVersionMeta m_currentMeta = new DataWranglerShotVersionMeta();
		private List<UserControl> m_currentFileSourceControls = new List<UserControl>();

		public ShotVersionFileSourcesControl()
		{
			InitializeComponent();
		}

		public void SetCurrentMeta(DataWranglerShotVersionMeta a_meta)
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
				foreach (DataWranglerFileSourceMeta fs in m_currentMeta.FileSources)
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
			FileSourceControl.Children.Add(new DataWranglerFileSourceUIDecorator(a_metaEditorControl, null));
		}
	}
}
