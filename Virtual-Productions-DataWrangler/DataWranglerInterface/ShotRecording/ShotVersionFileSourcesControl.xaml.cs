using System.Windows;
using System.Windows.Controls;
using CommonLogging;
using DataApiCommon;
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

	    public static readonly DependencyProperty SourceMetaProperty =
		    DependencyProperty.Register(
			    name: nameof(SourceMeta),
			    propertyType: typeof(IngestDataShotVersionMeta),
			    ownerType: typeof(ShotVersionFileSourcesControl),
			    typeMetadata: new FrameworkPropertyMetadata(
				    defaultValue: new IngestDataShotVersionMeta(),
				    flags: FrameworkPropertyMetadataOptions.AffectsRender,
				    propertyChangedCallback: OnSourceMetaChanged)
		    );

	    public IngestDataShotVersionMeta SourceMeta
	    {
		    get => (IngestDataShotVersionMeta)GetValue(SourceMetaProperty);
		    set => SetValue(SourceMetaProperty, value);
	    }

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

		public void SetCurrentMeta(IngestDataShotVersionMeta a_meta)
		{
		}

		public void UpdateDisplayedWidgets()
		{
			Dispatcher.InvokeAsync(() =>
			{
				FileSourceControl.Children.Clear();
				foreach (IngestDataSourceMeta fs in SourceMeta.FileSources)
				{
					DataWranglerFileSourceUIDecorator control = new DataWranglerFileSourceUIDecorator(fs, () => { RemoveMeta(fs); }, IsTemplateDisplay);
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
			IngestDataSourceMeta? meta = SourceMeta.FindMetaByType(a_meta.SourceType);
			if (meta == null)
			{
				SourceMeta.AddFileSource(a_meta);
				UpdateDisplayedWidgets();
			}
			else
			{
				Logger.LogWarning("UI", $"Could not add meta of type {meta.SourceType} as this is marked as being a unique meta.");
			}
		}

		private void RemoveMeta(IngestDataSourceMeta a_meta)
		{
			bool success = SourceMeta.RemoveFileSourceInstance(a_meta);
			if (!success)
			{
				throw new Exception("Failed to remove meta from file sources");
			}

			UpdateDisplayedWidgets();
		}

		private static void OnSourceMetaChanged(DependencyObject a_d, DependencyPropertyChangedEventArgs a_e)
		{
			//IngestDataShotVersionMeta newValue = (IngestDataShotVersionMeta)a_d.GetValue(SourceMetaProperty);
			ShotVersionFileSourcesControl target = (ShotVersionFileSourcesControl)a_d;

			if (target.SourceMeta != null)
			{
				target.UpdateDisplayedWidgets();
			}
		}

		private static void OnIsTemplateDisplayChanged(DependencyObject a_d, DependencyPropertyChangedEventArgs a_e)
		{
			bool newValue = (bool)a_d.GetValue(IsTemplateDisplayProperty);
			ShotVersionFileSourcesControl target = (ShotVersionFileSourcesControl) a_d;

			if (newValue)
			{
				target.AddFileSourceButton.Visibility = Visibility.Visible;
			}
			else
			{
				target.AddFileSourceButton.Visibility = Visibility.Hidden;
			}
		}
	}
}
