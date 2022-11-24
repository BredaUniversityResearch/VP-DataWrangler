using System.Windows;
using System.Windows.Controls;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for DataWranglerFileSourceUIDecorator.xaml
	/// </summary>
	public partial class DataWranglerFileSourceUIDecorator : UserControl
	{
		public DataWranglerFileSourceUIDecorator(UserControl a_childControl, Action? a_onRemoveAction)
		{
			InitializeComponent();
			ContentContainer.Children.Add(a_childControl);

			if (a_onRemoveAction != null)
			{
				RemoveMetaButton.Click += (_, _) => { a_onRemoveAction.Invoke(); };
			}
			else
			{
				RemoveMetaButton.Visibility = Visibility.Hidden;
			}

			if (a_childControl is IDataWranglerFileSourceUITitleProvider titleProvider)
			{
				FileSourceMeta.Content = titleProvider.FileSourceTitle;
			}
		}
	}
}
