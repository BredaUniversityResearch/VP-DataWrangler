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

			public ProjectSelectionEntry(int a_projectId, string a_projectName)
			{
				ProjectId = a_projectId;
				ProjectName = a_projectName;
			}

			public override string ToString()
			{
				return ProjectName;
			}
		};

		public delegate void SelectedProjectChangedDelegate(int projectId, string projectName);
		public event SelectedProjectChangedDelegate OnSelectedProjectChanged = delegate { };

		public ProjectSelectorControl()
		{
			InitializeComponent();

			ProjectListDropDown.SelectionChanged += ProjectListDropDownOnSelectionChanged;
		}

		private void ProjectListDropDownOnSelectionChanged(object a_sender, SelectionChangedEventArgs a_e)
		{
			ProjectSelectionEntry entry = (ProjectSelectionEntry) ProjectListDropDown.SelectedItem;
			OnSelectedProjectChanged.Invoke(entry.ProjectId, entry.ProjectName);
		}

		public void AsyncRefreshProjects()
		{
			DataWranglerServiceProvider.Instance.ShotGridAPI.GetProjects().ContinueWith(a_task => {

				if (a_task.Result != null)
				{
					ProjectListDropDown.Dispatcher.Invoke(() =>
					{
						foreach (ShotGridEntityProject project in a_task.Result)
						{
							if (project.Attributes.Name != null)
							{
								ProjectListDropDown.Items.Add(new ProjectSelectionEntry(project.Id,
									project.Attributes.Name));
							}
						}
					});
				}
			});
		}
	}
}
