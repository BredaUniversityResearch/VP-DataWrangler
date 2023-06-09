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
			if (a_shotInfo != null)
			{
				DisplayedShot = a_shotInfo;
				if (!string.IsNullOrEmpty(a_shotInfo.ImageURL))
				{
					ShotThumbnail.Source = new BitmapImage(new Uri(a_shotInfo.ImageURL));
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
				Task<DataApiResponseGeneric> task = m_displayedShot.ChangeTracker.CommitChanges(DataWranglerServiceProvider.Instance.TargetDataApi);
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
