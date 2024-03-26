using System.Windows.Controls;
using DataApiCommon;

namespace DataWranglerInterface.ShotRecording
{
	public partial class ProjectSelectorControl : UserControl
	{
		public class ProjectSelectionEntry
		{
			public readonly DataEntityProject Project;

			public ProjectSelectionEntry(DataEntityProject a_project)
			{
				Project = a_project;
			}

			public override string ToString()
			{
				return Project.Name;
			}
		};

		public delegate void SelectedProjectChangedDelegate(DataEntityProject a_selectedProject);
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
			SelectedProjectId = entry.Project.EntityId;
			OnSelectedProjectChanged.Invoke(entry.Project);
		}

		public void AsyncRefreshProjects()
		{
			ProjectListDropDown.BeginAsyncDataRefresh<DataEntityProject, ProjectSelectionEntry>(DataWranglerServiceProvider.Instance.TargetDataApi.GetActiveProjects());
		}
	}
}
