using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ShotGridIntegration;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for ShotCreationControl.xaml
	/// </summary>
	public partial class ShotCreationControl : UserControl
	{
		public delegate void RequestCreateNewShotDelegate(ShotGridEntityShotAttributes a_gridEntityShotAttributes);
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
			ShotGridEntityShotAttributes gridEntityShotAttributes = new ShotGridEntityShotAttributes();
			gridEntityShotAttributes.ShotCode = ShotNameInput.Text;
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
