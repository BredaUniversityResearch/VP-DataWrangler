using System.Windows.Controls;
using DataApiCommon;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for ShotVersionSelectorControl.xaml
	/// </summary>
	public partial class ShotVersionSelectorControl : UserControl
	{
		private class ShotVersionSelectorEntry
		{
			public DataEntityShotVersion ShotVersionInfo;

			public ShotVersionSelectorEntry(DataEntityShotVersion a_shotVersion)
			{
				ShotVersionInfo = a_shotVersion;
			}

			public override string ToString()
			{
				return ShotVersionInfo.ShotVersionName;
			}
		};

		public delegate void ShotVersionSelected(DataEntityShotVersion? a_shotVersion);
		public event ShotVersionSelected OnShotVersionSelected = delegate { };
		public int SelectedVersionEntityId { get; private set; } = -1;

		public ShotVersionSelectorControl()
		{
			InitializeComponent();
			ShotVersionSelectorDropDown.DropDown.SelectionChanged += OnVersionSelectionChanged;
		}

		private void OnVersionSelectionChanged(object a_sender, SelectionChangedEventArgs a_e)
		{
			ShotVersionSelectorEntry? version = (ShotVersionSelectorEntry?)ShotVersionSelectorDropDown.DropDown.SelectedItem;
			OnShotVersionSelected.Invoke(version?.ShotVersionInfo);
			SelectedVersionEntityId = version?.ShotVersionInfo.EntityId ?? -1;
		}

		public void AsyncRefreshShotVersion(int a_shotId)
		{
			SelectedVersionEntityId = -1;
			ShotVersionSelectorDropDown.BeginAsyncDataRefresh<DataEntityShotVersion, ShotVersionSelectorEntry>(DataWranglerServiceProvider.Instance.TargetDataApi.GetVersionsForShot(a_shotId));
		}

		public int GetHighestVersionNumber()
		{
			int highestVersionId = 0;
			foreach (ShotVersionSelectorEntry entry in ShotVersionSelectorDropDown.DropDown.Items)
			{
				string versionCode = entry.ShotVersionInfo.ShotVersionName;
				int lastSpacePos = versionCode.LastIndexOf(' ');
				if (lastSpacePos != -1)
				{
					string versionIdAsString = versionCode.Substring(lastSpacePos);
					if (int.TryParse(versionIdAsString, out int versionId))
					{
						highestVersionId = versionId;
					}
				}
			}

			return highestVersionId;
		}

		public void BeginAddShotVersion()
		{
			ShotVersionSelectorDropDown.SetLoading(true);
		}

		public void EndAddShotVersion(DataEntityShotVersion a_resultResult)
		{
			Dispatcher.Invoke(() =>
			{
				int index = ShotVersionSelectorDropDown.DropDown.Items.Add(new ShotVersionSelectorEntry(a_resultResult));
				ShotVersionSelectorDropDown.DropDown.SelectedIndex = index;
				ShotVersionSelectorDropDown.SetLoading(false);
			});
		}

		public void UpdateEntity(DataEntityShotVersion a_resultResultData)
		{
			foreach (ShotVersionSelectorEntry entry in ShotVersionSelectorDropDown.DropDown.Items)
			{
				if (entry.ShotVersionInfo.EntityId == a_resultResultData.EntityId)
				{
					entry.ShotVersionInfo = a_resultResultData;
					return;
				}
			}

			throw new Exception("Could not find entry to update");
		}
	}
}
