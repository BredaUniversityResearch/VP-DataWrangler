using System.Windows.Controls;
using System.Windows.Threading;
using ShotGridIntegration;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for ShotVersionSelectorControl.xaml
	/// </summary>
	public partial class ShotVersionSelectorControl : UserControl
	{
		private class ShotVersionSelectorEntry
		{
			public ShotGridEntityShotVersion ShotVersionInfo;

			public ShotVersionSelectorEntry(ShotGridEntityShotVersion a_shotVersion)
			{
				ShotVersionInfo = a_shotVersion;
			}

			public override string ToString()
			{
				return ShotVersionInfo.Attributes.VersionCode;
			}
		};

		public delegate void ShotVersionSelected(ShotGridEntityShotVersion? a_shotVersion);
		public event ShotVersionSelected OnShotVersionSelected = delegate { };
		public int CurrentVersionEntityId => ((ShotVersionSelectorEntry)ShotVersionSelectorDropDown.DropDown.SelectedItem).ShotVersionInfo.Id;
		
		public ShotVersionSelectorControl()
		{
			InitializeComponent();
			ShotVersionSelectorDropDown.DropDown.SelectionChanged += OnVersionSelectionChanged;
		}


		private void OnVersionSelectionChanged(object a_sender, SelectionChangedEventArgs a_e)
		{
			ShotVersionSelectorEntry? version = (ShotVersionSelectorEntry?)ShotVersionSelectorDropDown.DropDown.SelectedItem;
			OnShotVersionSelected.Invoke(version?.ShotVersionInfo);
		}

		public void AsyncRefreshShotVersion(int a_shotId)
		{
			ShotVersionSelectorDropDown.BeginAsyncDataRefresh<ShotGridEntityShotVersion, ShotVersionSelectorEntry>(DataWranglerServiceProvider.Instance.ShotGridAPI.GetVersionsForShot(a_shotId));
		}

		public int GetHighestVersionNumber()
		{
			int highestVersionId = 0;
			foreach (ShotVersionSelectorEntry entry in ShotVersionSelectorDropDown.DropDown.Items)
			{
				string versionCode = entry.ShotVersionInfo.Attributes.VersionCode;
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

		public void EndAddShotVersion(ShotGridEntityShotVersion a_resultResult)
		{
			Dispatcher.Invoke(() =>
			{
				int index = ShotVersionSelectorDropDown.DropDown.Items.Add(new ShotVersionSelectorEntry(a_resultResult));
				ShotVersionSelectorDropDown.DropDown.SelectedIndex = index;
				ShotVersionSelectorDropDown.SetLoading(false);
			});
		}

		public void UpdateEntity(ShotGridEntityShotVersion a_resultResultData)
		{
			foreach (ShotVersionSelectorEntry entry in ShotVersionSelectorDropDown.DropDown.Items)
			{
				if (entry.ShotVersionInfo.Id == a_resultResultData.Id)
				{
					entry.ShotVersionInfo = a_resultResultData;
					return;
				}
			}

			throw new Exception("Could not find entry to update");
		}
	}
}
