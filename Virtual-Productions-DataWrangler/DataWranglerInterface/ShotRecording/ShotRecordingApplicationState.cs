using DataApiCommon;

namespace DataWranglerInterface.ShotRecording
{
	public class ShotRecordingApplicationState
	{
		public event Action<DataEntityProject?>? OnSelectedProjectChanged;
		public event Action<DataEntityShot?>? OnSelectedShotChanged;
		public event Action<DataEntityShotVersion?>? OnSelectedShotVersionChanged;
		public event Action<string>? OnPredictedNextShotVersionNameChanged;

		public DataEntityProject? SelectedProject { get; private set; } = null;
		public DataEntityShot? SelectedShot { get; private set; } = null;
		public DataEntityShotVersion? SelectedShotVersion { get; private set; } = null;
		public string NextPredictedShotVersionName { get; private set; } = "";

		public void ProjectSelectionChanged(DataEntityProject? a_project)
		{
			SelectedProject = a_project;
			OnSelectedProjectChanged?.Invoke(a_project);
		}

		public void SelectedShotChanged(DataEntityShot? a_shotInfo)
		{
			SelectedShot = a_shotInfo;
			OnSelectedShotChanged?.Invoke(a_shotInfo);
		}

		public void NextShotVersionNameChanged(string a_nextShotVersionName)
		{
			NextPredictedShotVersionName = a_nextShotVersionName;
			OnPredictedNextShotVersionNameChanged?.Invoke(NextPredictedShotVersionName);
		}

		public void SelectedShotVersionChanged(DataEntityShotVersion? a_shotVersion)
		{
			SelectedShotVersion = a_shotVersion;
			OnSelectedShotVersionChanged?.Invoke(a_shotVersion);
		}
	}
}
