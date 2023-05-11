using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using AutoNotify;
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
		private ShotGridEntityShotAttributes? m_displayedAttributes = null;

		private ShotGridEntityShot? m_displayedShot = null;

		public ShotInfoDisplay()
		{
			InitializeComponent();
		}
		
		public void SetDisplayedShot(ShotGridEntityShot? a_shotInfo)
		{
			if (a_shotInfo != null)
			{
				m_displayedShot = a_shotInfo;
				DisplayedAttributes = a_shotInfo.Attributes;
				if (!string.IsNullOrEmpty(a_shotInfo.Attributes.ImageURL))
				{
					ShotThumbnail.Source = new BitmapImage(new Uri(a_shotInfo.Attributes.ImageURL));
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

		private void OnDescriptionChanged(object a_sender, RoutedEventArgs a_e)
		{
			if (m_displayedShot != null && m_displayedShot.ChangeTracker.HasAnyUncommittedChanges())
			{
				Task<ShotGridAPIResponseGeneric> task = m_displayedShot.ChangeTracker.CommitChanges(DataWranglerServiceProvider.Instance.ShotGridAPI);
				FrameworkElement sender = (FrameworkElement)a_sender;
				AsyncOperationChangeFeedback? feedbackElement = AsyncOperationChangeFeedback.FindFeedbackElementFrom(sender);
				if (feedbackElement != null)
				{
					feedbackElement.ProvideFeedback(task);
				}
			}
		}
	}
}
