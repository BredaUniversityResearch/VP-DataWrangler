using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using AutoNotify;
using DataApiCommon;
using DataWranglerCommonWPF;
using ShotGridIntegration;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for ShotInfoDisplay.xaml
	/// </summary>
	public partial class ShotInfoDisplay : UserControl
	{
		[AutoNotify]
		private DataEntityShot? m_displayedShot = null;

		public ShotInfoDisplay()
		{
			InitializeComponent();
		}
		
		public void SetDisplayedShot(DataEntityShot? a_shotInfo)
		{
			if (DisplayedShot != null)
			{
				DisplayedShot.PropertyChanged -= OnDisplayedShotPropertyChanged;
			}

			DisplayedShot = a_shotInfo;
			if (DisplayedShot != null)
			{
				DisplayedShot.PropertyChanged += OnDisplayedShotPropertyChanged;
				
				if (!string.IsNullOrEmpty(DisplayedShot.ImageURL))
				{
					ShotThumbnail.Source = new BitmapImage(new Uri(DisplayedShot.ImageURL));
				}
				else
				{
					ShotThumbnail.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/MissingThumbnail.png"));
				}
			}
			else
			{
				ShotCode.Content = "";
				ShotThumbnail.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/MissingThumbnail.png"));
			}
		}

		private void OnDisplayedShotPropertyChanged(object? a_sender, PropertyChangedEventArgs a_e)
		{
			if (m_displayedShot == null)
			{
				return;
			}

			if (a_e.PropertyName == nameof(DisplayedShot.Description))
			{
				Task<DataApiResponseGeneric> task = m_displayedShot.ChangeTracker.CommitChanges(DataWranglerServiceProvider.Instance.TargetDataApi);
				DescriptionFeedbackElement.ProvideFeedback(task);
			}
		}
	}
}
