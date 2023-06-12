using System.Windows;
using System.Windows.Controls;
using DataApiCommon;
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
			public readonly Guid ShotId;
			public readonly DataEntityShot ShotInfo;

			public ShotSelectorEntry(DataEntityShot a_shotInfo)
			{
				ShotId = a_shotInfo.EntityId;
				ShotInfo = a_shotInfo;
			}

			public override string ToString()
			{
				return ShotInfo.ShotName;
			}
		};

		public delegate void ShotSelectionChangedDelegate(DataEntityShot? a_shotInfo);
		public event ShotSelectionChangedDelegate OnSelectedShotChanged = delegate { };

		public event Action OnNewShotCreatedButtonClicked = delegate { };

		public Guid SelectedShotId { get; private set; }

		public ShotSelectorControl()
		{
			InitializeComponent();

			ShotSelectorDropDown.DropDown.SelectionChanged += OnShotSelectionChanged;
		}

		private void OnShotSelectionChanged(object a_sender, SelectionChangedEventArgs a_e)
		{
			ShotSelectorEntry? entry = (ShotSelectorEntry?) ShotSelectorDropDown.DropDown.SelectedItem;
			OnSelectedShotChanged.Invoke(entry?.ShotInfo);
			SelectedShotId = entry?.ShotId ?? Guid.Empty;
		}

		public void AsyncRefreshShots(Guid a_projectId)
		{
			ShotSelectorDropDown.BeginAsyncDataRefresh<DataEntityShot, ShotSelectorEntry>(DataWranglerServiceProvider.Instance.TargetDataApi.GetShotsForProject(a_projectId));
		}

		public void OnNewShotCreationFinished(DataEntityShot? a_resultResultData)
		{
			if (a_resultResultData != null)
			{
				ShotSelectorDropDown.AddDropdownEntry<DataEntityShot, ShotSelectorEntry>(a_resultResultData, true);
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
