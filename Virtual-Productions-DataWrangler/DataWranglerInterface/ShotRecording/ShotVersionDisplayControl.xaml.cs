using System.Windows;
using System.Windows.Controls;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for ShotVersionDisplayControl.xaml
	/// </summary>
	public partial class ShotVersionDisplayControl : UserControl
	{
		private int SelectedShotId = -1;

		public ShotVersionDisplayControl()
		{
			InitializeComponent();

			CreateNewTake.Click += OnCreateNewTake;
		}

		private void OnCreateNewTake(object a_sender, RoutedEventArgs a_e)
		{
			if (SelectedShotId != -1)
			{
				int nextTakeId = VersionSelectorControl.GetHighestVersionNumber() + 1;
				DataWranglerServiceProvider.Instance.ShotGridAPI.CreateNewShotVersion(SelectedShotId,
					$"Take {nextTakeId:D2}");
			}
		}

		public void OnShotSelected(int a_shotId)
		{
			SelectedShotId = a_shotId;
			VersionSelectorControl.AsyncRefreshShotVersion(a_shotId);
		}
	}
}
