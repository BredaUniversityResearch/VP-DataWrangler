using System.Windows.Controls;
using ShotGridIntegration;

namespace DataWranglerInterface.ShotRecording
{
	public partial class ProjectSelectorControl : UserControl
	{
		public class ProjectSelectionEntry
		{
			public readonly int ProjectId;
			public readonly string ProjectName;

			public ProjectSelectionEntry(ShotGridEntityProject a_project)
			{
				ProjectId = a_project.Id;
				ProjectName = a_project.Attributes.Name;
			}

			public override string ToString()
			{
				return ProjectName;
			}
		};

		public delegate void SelectedProjectChangedDelegate(int projectId, string projectName);
		public event SelectedProjectChangedDelegate OnSelectedProjectChanged = delegate { };

		public int SelectedProjectId => ((ProjectSelectionEntry) ProjectListDropDown.DropDown.SelectedItem).ProjectId;

		public ProjectSelectorControl()
		{
			InitializeComponent();

			ProjectListDropDown.DropDown.SelectionChanged += DropDownOnSelectionChanged;
		}

		private void DropDownOnSelectionChanged(object a_sender, SelectionChangedEventArgs a_e)
		{
			ProjectSelectionEntry entry = (ProjectSelectionEntry) ProjectListDropDown.DropDown.SelectedItem;
			OnSelectedProjectChanged.Invoke(entry.ProjectId, entry.ProjectName);
		}

		public void AsyncRefreshProjects()
		{
			ProjectListDropDown.BeginAsyncDataRefresh<ShotGridEntityProject, ProjectSelectionEntry>(DataWranglerServiceProvider.Instance.ShotGridAPI.GetActiveProjects());
		}
	}
}
