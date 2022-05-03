using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ShotGridIntegration;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for ShotInfoDisplay.xaml
	/// </summary>
	public partial class ShotInfoDisplay : UserControl
	{
		public ShotInfoDisplay()
		{
			InitializeComponent();
		}


		public void SetDisplayedShot(ShotGridEntityShot? a_shotInfo)
		{
			if (a_shotInfo != null)
			{
				ShotCode.Content = a_shotInfo.Attributes.ShotCode;
				Description.Text = a_shotInfo.Attributes.Description;
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
