using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DataWranglerCommonWPF
{
	/// <summary>
	/// Interaction logic for LoadingSpinnerInstance.xaml
	/// </summary>
	public partial class LoadingSpinner : UserControl
	{
		private static readonly TimeSpan LongDurationSpinTime = TimeSpan.FromSeconds(5);
		private readonly CancellationTokenSource m_cancellationTokenSource = new CancellationTokenSource();

		public double SpinnerCoverOpacity { get; set; } = 0.0;

		public LoadingSpinner()
		{
			InitializeComponent();
			Unloaded += LoadingSpinner_Unloaded;
		}

		protected override void OnVisualParentChanged(DependencyObject oldParent)
		{
			base.OnVisualParentChanged(oldParent);
		}

		public void SetIsLoading(bool a_isLoading)
		{
			Visibility = a_isLoading ? Visibility.Visible : Visibility.Hidden;
			if (a_isLoading)
			{
				Spinner.Source = (ImageSource)Resources["SpinnerImageSource"];
				Task.Delay(LongDurationSpinTime, m_cancellationTokenSource.Token).ContinueWith(_ =>
				{
					Dispatcher.Invoke(() =>
					{
						Spinner.Source = (ImageSource)Resources["SpinnerImageLongDuration"];
					});
				});
			}
		}

		private void LoadingSpinner_Unloaded(object a_sender, RoutedEventArgs a_e)
		{
			m_cancellationTokenSource.Cancel();
		}
	}
}
