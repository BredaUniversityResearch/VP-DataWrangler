using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

		private void SetupInputFieldsFromAttributes(DataEntityShot a_attributes)
		{
			ShotNameInput.Text = a_attributes.ShotName;
		}

		private void ResetInputFields()
		{
			ShotNameInput.Text = "";
		}

		private void HideAndReset()
		{
			ResetInputFields();
			Visibility = Visibility.Hidden;
			ErrorBox.Visibility = Visibility.Collapsed;
		}

		private void ButtonCreate_Click(object a_sender, RoutedEventArgs a_e)
		{
			DataEntityShot gridEntityShotAttributes = new DataEntityShot
			{
				ShotName = ShotNameInput.Text
			};
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

		public void OnNewShotCreationFailed(DataEntityShot a_failedCreationAttributes, string a_errorMessage)
		{
			SetupInputFieldsFromAttributes(a_failedCreationAttributes);

			ErrorBox.Visibility = Visibility.Visible;
			ErrorMessage.Content = a_errorMessage;

			Show();
		}

		private void ShotNameInput_OnKeyDown(object a_sender, KeyEventArgs a_e)
		{
			if (a_e.Key == Key.Enter)
			{
				ButtonCreate_Click(a_sender, a_e);
			}
		}
	}
}
