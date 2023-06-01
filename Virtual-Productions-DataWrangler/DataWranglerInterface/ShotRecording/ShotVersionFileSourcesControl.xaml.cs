using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using CommonLogging;
using DataWranglerCommon;
using DataWranglerCommon.IngestDataSources;

namespace DataWranglerInterface.ShotRecording
{
    /// <summary>
    /// Interaction logic for ShotVersionFileSourcesControl.xaml
    /// </summary>
    public partial class ShotVersionFileSourcesControl : UserControl
    {
	    public static readonly DependencyProperty IsTemplateDisplayProperty =
		    DependencyProperty.Register(
			    name: nameof(IsTemplateDisplay),
			    propertyType: typeof(bool),
			    ownerType: typeof(ShotVersionFileSourcesControl),
			    typeMetadata: new FrameworkPropertyMetadata(
				    defaultValue: false,
				    flags: FrameworkPropertyMetadataOptions.AffectsRender,
					propertyChangedCallback: OnIsTemplateDisplayChanged)
		    );

	    public bool IsTemplateDisplay
	    { 
		    get => (bool)GetValue(IsTemplateDisplayProperty); 
		    set => SetValue(IsTemplateDisplayProperty, value); 
	    }

		private IngestDataShotVersionMeta m_currentDisplayedMeta = new IngestDataShotVersionMeta();

		private ContextMenu m_addFileSourceContextMenu;

		public ShotVersionFileSourcesControl()
		{
			m_addFileSourceContextMenu = new ContextMenu();
			MenuItem item = new MenuItem
			{
				Header = "Blackmagic Ursa"
			};
			item.Click += (_, _) => TryAddSource(new IngestDataSourceMetaBlackmagicUrsa());
			m_addFileSourceContextMenu.Items.Add(item);
			
			item = new MenuItem
			{
				Header = "TASCAM DR-60 MkII"
			};
			item.Click += (_, _) => TryAddSource(new IngestDataSourceMetaTascam());
			m_addFileSourceContextMenu.Items.Add(item);
			
			item = new MenuItem
			{
				Header = "Vicon Motion Data"
			};
			item.Click += (_, _) => TryAddSource(new IngestDataSourceMetaViconTracking());
			m_addFileSourceContextMenu.Items.Add(item);

			InitializeComponent();

			AddFileSourceButton.Visibility = Visibility.Hidden;
		}

		public IngestDataShotVersionMeta CreateMetaFromCurrentTemplate()
		{
			IngestDataShotVersionMeta meta = m_currentDisplayedMeta.Clone();
			return meta;
		}

		public void SetCurrentMeta(IngestDataShotVersionMeta a_meta)
		{
			m_currentDisplayedMeta = a_meta;
			UpdateDisplayedWidgets();
		}

		public void UpdateDisplayedWidgets()
		{
			Dispatcher.InvokeAsync(() =>
			{
				FileSourceControl.Children.Clear();
				foreach (IngestDataSourceMeta fs in m_currentDisplayedMeta.FileSources)
				{
					DataWranglerFileSourceUIDecorator control = new DataWranglerFileSourceUIDecorator(fs, () => { RemoveMeta(fs); });
					FileSourceControl.Children.Add(control);
				}
			});
		}

		private void AddFileSourceButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			m_addFileSourceContextMenu.PlacementTarget = AddFileSourceButton;
			m_addFileSourceContextMenu.IsOpen = true;
		}

		private void TryAddSource(IngestDataSourceMeta a_meta)
		{
			IngestDataSourceMeta? meta = m_currentDisplayedMeta.FileSources.Find(source => source.SourceType == a_meta.SourceType);
			if (meta == null)
			{
				m_currentDisplayedMeta.FileSources.Add(a_meta);
				UpdateDisplayedWidgets();
			}
			else
			{
				Logger.LogWarning("UI", $"Could not add meta of type {meta.SourceType} as this is marked as being a unique meta.");
			}
		}

		private void RemoveMeta(IngestDataSourceMeta a_meta)
		{
			bool success = m_currentDisplayedMeta.FileSources.Remove(a_meta);
			if (!success)
			{
				throw new Exception("Failed to remove meta from file sources");
			}

			UpdateDisplayedWidgets();
		}

		private static void OnIsTemplateDisplayChanged(DependencyObject a_d, DependencyPropertyChangedEventArgs a_e)
		{
			bool newValue = (bool)a_d.GetValue(IsTemplateDisplayProperty);
			ShotVersionFileSourcesControl target = (ShotVersionFileSourcesControl) a_d;

			if (newValue)
			{
				target.m_currentDisplayedMeta = new IngestDataShotVersionMeta();
				target.m_currentDisplayedMeta.FileSources.Add(new IngestDataSourceMetaBlackmagicUrsa());
				target.UpdateDisplayedWidgets();
				
				target.AddFileSourceButton.Visibility = Visibility.Visible;
			}
			else
			{
				target.AddFileSourceButton.Visibility = Visibility.Hidden;
			}
		}
	}
}
