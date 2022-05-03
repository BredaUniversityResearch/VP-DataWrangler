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
			public int VersionEntityId;
			public ShotGridEntityShotVersion ShotVersionInfo;

			public ShotVersionSelectorEntry(int a_versionEntityId, ShotGridEntityShotVersion a_shotVersion)
			{
				VersionEntityId = a_versionEntityId;
				ShotVersionInfo = a_shotVersion;
			}

			public override string ToString()
			{
				return ShotVersionInfo.Attributes.VersionCode;
			}
		};

		public ShotVersionSelectorControl()
		{
			InitializeComponent();
		}

		public void AsyncRefreshShotVersion(int a_shotId)
		{
			ShotVersionSelectorDropDown.Dispatcher.Invoke(() =>
			{
				ShotVersionSelectorDropDown.Items.Clear();
			});

			if (a_shotId != -1)
			{
				DataWranglerServiceProvider.Instance.ShotGridAPI.GetVersionsForShot(a_shotId).ContinueWith(a_task =>
				{
					if (a_task.Result != null)
					{
						ShotVersionSelectorDropDown.Dispatcher.Invoke(() =>
						{
							foreach (ShotGridEntityShotVersion shot in a_task.Result)
							{
								ShotVersionSelectorDropDown.Items.Add(new ShotVersionSelectorEntry(shot.Id, shot));
							}
						});
					}
				});
			}
		}

		public int GetHighestVersionNumber()
		{
			int highestVersionId = 0;
			foreach (ShotVersionSelectorEntry entry in ShotVersionSelectorDropDown.Items)
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
	}
}
