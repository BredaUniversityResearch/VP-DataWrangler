using System.Windows;
using System.Windows.Controls;
using DataApiCommon;
using ShotGridIntegration;

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

		public void BeginAsyncDataRefresh<TAPIResult, TDropDownType>(Task<DataApiResponse<TAPIResult[]>> a_task)
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

				if (a_results.IsCompletedSuccessfully)
				{
					DataApiResponse<TAPIResult[]> apiResults = a_results.Result;
					TDropDownType[] result = Array.Empty<TDropDownType>();
					if (!apiResults.IsError)
					{
						result = new TDropDownType[apiResults.ResultData.Length];
						for (int i = 0; i < result.Length; ++i)
						{
							TDropDownType? dropDownType = (TDropDownType?)Activator.CreateInstance(typeof(TDropDownType), apiResults.ResultData[i]);
							result[i] = dropDownType ?? throw new Exception($"Failed to create object {typeof(TDropDownType).Name} with {typeof(TAPIResult).Name} as first argument");
						}
					}
					
					AssignItemsToDropdown(result);
				}
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

		public void SetLoading(bool a_isLoading)
		{
			if (Dispatcher.CheckAccess())
			{
				DropDown.IsEnabled = !a_isLoading;
				LoadingSpinner.SetIsLoading(a_isLoading);
			}
			else
			{
				Dispatcher.InvokeAsync(() => SetLoading(a_isLoading));
			}
		}

		public void AddDropdownEntry<TEntityType, TDropDownType>(TEntityType a_entityData, bool a_selectNewEntry)
		{
			if (Dispatcher.CheckAccess())
			{
				TDropDownType? dropDownType = (TDropDownType?)Activator.CreateInstance(typeof(TDropDownType), a_entityData);
				if (dropDownType == null)
				{
					throw new Exception($"Failed to create object {typeof(TDropDownType).Name} with {typeof(TEntityType).Name} as first argument");
				}

				DropDown.Items.Add(dropDownType);
				DropDown.SelectedIndex = DropDown.Items.Count - 1;
			}
			else
			{
				Dispatcher.InvokeAsync(() => AddDropdownEntry<TEntityType, TDropDownType>(a_entityData, a_selectNewEntry));
			}
		}
	}
}
