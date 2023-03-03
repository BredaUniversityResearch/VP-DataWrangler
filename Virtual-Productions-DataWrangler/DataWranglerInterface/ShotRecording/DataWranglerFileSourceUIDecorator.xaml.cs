using System.Windows;
using System.Windows.Controls;
using DataWranglerCommon;

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

		public static UserControl CreateEditorForMeta(DataWranglerFileSourceMeta a_meta)
		{
			if (a_meta is DataWranglerFileSourceMetaBlackmagicUrsa ursaSource)
			{
				return new DataWranglerFileSourceUIBlackmagicUrsa(ursaSource);
			}
			else if (a_meta is DataWranglerFileSourceMetaTascam tascam)
			{
				return new DataWranglerFileSourceUITascam(tascam);
			}

			throw new Exception($"CameraNumber meta source type {a_meta.GetType()}");
		}
	}
}
