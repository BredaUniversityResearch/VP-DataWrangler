using System.Windows;
using DataWranglerInterface.Login;

namespace DataWranglerInterface
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private readonly LoginPage m_loginPage;

		public MainWindow()
		{
			InitializeComponent();

			m_loginPage = new LoginPage();
			Content = m_loginPage;
		}
	}
}
