using System.Windows.Controls;
using ShotGridIntegration;

namespace DataWranglerInterface.ShotRecordingPage
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

		public ProjectSelectorControl()
		{
			InitializeComponent();
		}

		public void AsyncRefreshProjects()
		{
			DataWranglerServiceProvider.Instance.ShotGridAPI.GetProjects().ContinueWith(a_task => {

				if (a_task.Result != null)
				{
					foreach (ShotGridEntityProject project in a_task.Result)
					{
						if (project.Attributes.Name != null)
						{
							ProjectListDropDown.Items.Add(new ProjectSelectionEntry(project.Id, project.Attributes.Name));
						}
					}
				}
			});
		}
	}
}
