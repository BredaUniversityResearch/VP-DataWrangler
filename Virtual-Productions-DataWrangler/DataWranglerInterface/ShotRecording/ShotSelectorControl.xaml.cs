using System.Windows;
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

			public ShotSelectorEntry(ShotGridEntityShot a_shotInfo)
			{
				ShotId = a_shotInfo.Id;
				ShotInfo = a_shotInfo;
			}

			public override string ToString()
			{
				return ShotInfo.Attributes.ShotCode!;
			}
		};

		public delegate void ShotSelectionChangedDelegate(ShotGridEntityShot? a_shotInfo);
		public event ShotSelectionChangedDelegate OnSelectedShotChanged = delegate { };

		public event Action OnNewShotCreatedButtonClicked = delegate { };

		public int SelectedShotId { get; private set; }

		public ShotSelectorControl()
		{
			InitializeComponent();

			ShotSelectorDropDown.DropDown.SelectionChanged += OnShotSelectionChanged;
		}

		private void OnShotSelectionChanged(object a_sender, SelectionChangedEventArgs a_e)
		{
			ShotSelectorEntry? entry = (ShotSelectorEntry?) ShotSelectorDropDown.DropDown.SelectedItem;
			OnSelectedShotChanged.Invoke(entry?.ShotInfo);
			SelectedShotId = entry?.ShotId ?? -1;
		}

		public void AsyncRefreshShots(int a_projectId)
		{
			ShotSelectorDropDown.BeginAsyncDataRefresh<ShotGridEntityShot, ShotSelectorEntry>(DataWranglerServiceProvider.Instance.ShotGridAPI.GetShotsForProject(a_projectId));
		}

		public void OnNewShotCreationFinished(ShotGridEntityShot? a_resultResultData)
		{
			if (a_resultResultData != null)
			{
				ShotSelectorDropDown.AddDropdownEntry<ShotGridEntityShot, ShotSelectorEntry>(a_resultResultData, true);
			}

			ShotSelectorDropDown.SetLoading(false);
		}

		public void OnNewShotCreationStarted()
		{
			ShotSelectorDropDown.SetLoading(true);
		}

		private void ButtonNewShot_Click(object a_sender, RoutedEventArgs a_e)
		{
			OnNewShotCreatedButtonClicked?.Invoke();
		}
	}
}
