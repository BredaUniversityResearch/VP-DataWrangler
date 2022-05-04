using System.Windows;
using System.Windows.Controls;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for RemoteDataDropDown.xaml
	/// </summary>
	public partial class RemoteDataDropDown : UserControl
	{
		private Task? m_itemEntryRefreshTask;

		public RemoteDataDropDown()
		{
			InitializeComponent();
			SetLoading(false);
		}

		public void BeginAsyncDataRefresh<TAPIResult, TDropDownType>(Task<TAPIResult[]?> a_task)
		{
			if (m_itemEntryRefreshTask == null)
			{
				Dispatcher.Invoke(() =>
				{
					SetLoading(true);
					DropDown.Items.Clear();
					DropDown.IsEnabled = false;
				});
			}

			m_itemEntryRefreshTask = a_task;
			a_task.ContinueWith((a_results) => {
				if (a_results != m_itemEntryRefreshTask)
				{
					return;
				}

				TAPIResult[]? apiResults = a_results.Result;
				TDropDownType[] result = Array.Empty<TDropDownType>();
				if (apiResults != null)
				{
					result = new TDropDownType[apiResults.Length];
					for (int i = 0; i < result.Length; ++i)
					{
						TDropDownType? dropDownType = (TDropDownType?)Activator.CreateInstance(typeof(TDropDownType), apiResults[i]);
						result[i] = dropDownType ?? throw new Exception($"Failed to create object {typeof(TDropDownType).Name} with {typeof(TAPIResult).Name} as first argument");
					}
				}
				
				AssignItemsToDropdown(result);
				m_itemEntryRefreshTask = null;
			});
		}

		private void AssignItemsToDropdown<TDropDownType>(TDropDownType[] a_result) 
		{
			DropDown.Dispatcher.Invoke(() =>
			{
				foreach (TDropDownType dropDownEntry in a_result)
				{
					DropDown.Items.Add(dropDownEntry);
				}

				SetLoading(false);
			});
		}

		//public void BeginAsyncDataRefresh<TResultDataType>(Task<TResultDataType[]> a_task)
		//{
		//	if (m_itemEntryRefreshTask == null)
		//	{
		//		Dispatcher.Invoke(() =>
		//		{
		//			LoadingSpinner.Visibility = Visibility.Visible;
		//			DropDown.Items.Clear();
		//			DropDown.IsEnabled = false;
		//		});
		//	}

		//	m_itemEntryRefreshTask = a_task;
		//	a_task.ContinueWith(OnRefreshCompleted);
		//}

		//public void OnRefreshCompleted<TResultDataType>(Task<TResultDataType[]> a_resultTask)
		//{
		//	if (a_resultTask != m_itemEntryRefreshTask)
		//	{
		//		return;
		//	}

		//}
		public void SetLoading(bool a_isLoading)
		{
			if (Dispatcher.CheckAccess())
			{
				DropDown.IsEnabled = !a_isLoading;
				LoadingSpinner.Visibility = (a_isLoading)? Visibility.Visible : Visibility.Hidden;
			}
			else
			{
				Dispatcher.InvokeAsync(() => SetLoading(a_isLoading));
			}
		}
	}
}
