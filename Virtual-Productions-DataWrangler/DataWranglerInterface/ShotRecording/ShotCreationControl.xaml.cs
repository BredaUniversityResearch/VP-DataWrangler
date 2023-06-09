using System.Windows;
using System.Windows.Controls;
using DataApiCommon;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for ShotCreationControl.xaml
	/// </summary>
	public partial class ShotCreationControl : UserControl
	{
		public delegate void RequestCreateNewShotDelegate(DataEntityShot a_gridEntityShotAttributes);
		public event RequestCreateNewShotDelegate OnRequestCreateNewShot = delegate { };

		public ShotCreationControl()
		{
			InitializeComponent();
			HideAndReset();
		}

		private void ResetInputFields()
		{
			ShotNameInput.Text = "";
		}

		private void HideAndReset()
		{
			ResetInputFields();
			Visibility = Visibility.Hidden;
		}

		private void ButtonCreate_Click(object a_sender, RoutedEventArgs a_e)
		{
			DataEntityShot gridEntityShotAttributes = new DataEntityShot();
			gridEntityShotAttributes.ShotName = ShotNameInput.Text;
			OnRequestCreateNewShot.Invoke(gridEntityShotAttributes);

			HideAndReset();
		}

		private void ButtonCancel_Click(object a_sender, RoutedEventArgs a_e)
		{
			HideAndReset();
		}

		public void Show()
		{
			Visibility = Visibility.Visible;
		}
	}
}
