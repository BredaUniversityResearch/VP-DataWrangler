using System.Windows.Controls;
using System.Windows.Media.Imaging;
using AutoNotify;
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

		public ShotInfoDisplay()
		{
			InitializeComponent();
		}
		
		public void SetDisplayedShot(ShotGridEntityShot? a_shotInfo)
		{
			if (a_shotInfo != null)
			{
				DisplayedAttributes = a_shotInfo.Attributes;
				//ShotCode.Content = a_shotInfo.Attributes.ShotCode;
				//Description.Text = a_shotInfo.Attributes.Description;
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
				Description.Text = "";
				ShotThumbnail.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/MissingThumbnail.png"));
			}
		}
	}
}
