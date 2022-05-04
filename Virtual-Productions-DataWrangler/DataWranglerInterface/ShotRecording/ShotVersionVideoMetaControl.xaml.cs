using System.Windows.Controls;
using DataWranglerCommon;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for ShotVersionVideoMetaControl.xaml
	/// </summary>
	public partial class ShotVersionVideoMetaControl : UserControl
	{
		public DataWranglerShotVersionMeta.VideoMeta CurrentMeta { get; private set; } = new();

		public ShotVersionVideoMetaControl()
		{
			InitializeComponent();

			VideoSource.Items.Add("Alpha Blackmagic Ursa");
			VideoSource.Items.Add("Beta D90");
		}

		public void UpdateData(DataWranglerShotVersionMeta a_targetMeta)
		{
			CurrentMeta = a_targetMeta.Video;
			SetUIForCurrentMeta();
		}

		private void SetUIForCurrentMeta()
		{
			if (!Dispatcher.CheckAccess())
			{
				Dispatcher.InvokeAsync(SetUIForCurrentMeta);
				return;
			}

			VideoSource.SelectedItem = CurrentMeta.Source;

		}
	}
}
