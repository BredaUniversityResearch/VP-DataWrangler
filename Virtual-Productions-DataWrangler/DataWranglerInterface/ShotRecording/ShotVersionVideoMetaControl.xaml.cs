using System.Windows.Controls;
using AutoNotify;
using DataWranglerCommon;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for ShotVersionVideoMetaControl.xaml
	/// </summary>
	public partial class ShotVersionVideoMetaControl : UserControl
	{
		[AutoNotify]
		private DataWranglerVideoMeta m_currentMeta = new();

		public ShotVersionVideoMetaControl()
		{
			InitializeComponent();

			VideoSource.Items.Add("Giga Chad Blackmagic Ursa");
			VideoSource.Items.Add("Chad Pocket Cinema");
			VideoSource.Items.Add("Virgin D90");
		}

		public void UpdateData(DataWranglerShotVersionMeta a_targetMeta)
		{
			CurrentMeta = a_targetMeta.Video;
		}
	}
}
