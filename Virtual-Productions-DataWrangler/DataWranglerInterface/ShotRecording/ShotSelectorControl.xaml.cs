using System.Windows.Controls;
using ShotGridIntegration;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for ShotSelectorControl.xaml
	/// </summary>
	public partial class ShotSelectorControl : UserControl
	{
		private class ShotSelectorEntry
		{
			public readonly int ShotId;
			public readonly ShotGridEntityShot ShotInfo;

			public ShotSelectorEntry(int a_shotId, ShotGridEntityShot a_shotInfo)
			{
				ShotId = a_shotId;
				ShotInfo = a_shotInfo;
			}

			public override string ToString()
			{
				return ShotInfo.Attributes.ShotCode!;
			}
		};

		public delegate void ShotSelectionChangedDelegate(ShotGridEntityShot? a_shotInfo);
		public event ShotSelectionChangedDelegate OnSelectedShotChanged = delegate { };

		public ShotSelectorControl()
		{
			InitializeComponent();

			ShotSelectorDropDown.SelectionChanged += OnShotSelectionChanged;
		}

		private void OnShotSelectionChanged(object a_sender, SelectionChangedEventArgs a_e)
		{
			ShotSelectorEntry? entry = (ShotSelectorEntry?) ShotSelectorDropDown.SelectedItem;
			OnSelectedShotChanged.Invoke(entry?.ShotInfo);
		}

		public void AsyncRefreshShots(int a_projectId)
		{
			ShotSelectorDropDown.Dispatcher.Invoke(() =>
			{
				ShotSelectorDropDown.Items.Clear();
			});

			DataWranglerServiceProvider.Instance.ShotGridAPI.GetShotsForProject(a_projectId).ContinueWith(a_task =>
			{
				if (a_task.Result != null)
				{
					ShotSelectorDropDown.Dispatcher.Invoke(() =>
					{
						foreach (ShotGridEntityShot shot in a_task.Result)
						{
							ShotSelectorDropDown.Items.Add(new ShotSelectorEntry(shot.Id, shot));
						}
					});
				}
			});
		}
	}
}
