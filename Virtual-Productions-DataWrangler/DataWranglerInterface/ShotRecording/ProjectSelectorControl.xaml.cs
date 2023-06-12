using System.Windows.Controls;
using DataApiCommon;

namespace DataWranglerInterface.ShotRecording
{
	public partial class ProjectSelectorControl : UserControl
	{
		public class ProjectSelectionEntry
		{
			public readonly Guid ProjectId;
			public readonly string ProjectName;

			public ProjectSelectionEntry(DataEntityProject a_project)
			{
				ProjectId = a_project.EntityId;
				ProjectName = a_project.Name;
			}

			public override string ToString()
			{
				return ProjectName;
			}
		};

		public delegate void SelectedProjectChangedDelegate(Guid projectId, string projectName);
		public event SelectedProjectChangedDelegate OnSelectedProjectChanged = delegate { };

		public Guid SelectedProjectId { get; private set; }

		public ProjectSelectorControl()
		{
			InitializeComponent();

			ProjectListDropDown.DropDown.SelectionChanged += DropDownOnSelectionChanged;
		}

		private void DropDownOnSelectionChanged(object a_sender, SelectionChangedEventArgs a_e)
		{
			ProjectSelectionEntry entry = (ProjectSelectionEntry) ProjectListDropDown.DropDown.SelectedItem;
			SelectedProjectId = entry.ProjectId;
			OnSelectedProjectChanged.Invoke(entry.ProjectId, entry.ProjectName);
		}

		public void AsyncRefreshProjects()
		{
			ProjectListDropDown.BeginAsyncDataRefresh<DataEntityProject, ProjectSelectionEntry>(DataWranglerServiceProvider.Instance.TargetDataApi.GetActiveProjects());
		}
	}
}
