using System.Collections.ObjectModel;
using System.Windows.Controls;
using DataWranglerInterface.Configuration;

namespace DataWranglerInterface.ShotRecording
{
	public partial class CameraStorageTargetDropDown : UserControl
	{
		private readonly ObservableCollection<string> m_storageTargets = new ObservableCollection<string>();

		private string m_storageTargetStringCached;
		public string StorageTargetString
		{
			get => m_storageTargetStringCached;
			set
			{
				int storageTargetIndex = m_storageTargets.IndexOf(value);
				if (storageTargetIndex == -1)
				{
					m_storageTargets.Add(value);
					storageTargetIndex = m_storageTargets.Count - 1;
				}

				StorageTargetDropdown.SelectedIndex = storageTargetIndex;
				m_storageTargetStringCached = (string)StorageTargetDropdown.SelectedValue;
			}
		}

		public event Action<string> OnStorageTargetChanged = delegate {  };

		public CameraStorageTargetDropDown()
		{
			foreach(string storageTarget in DataWranglerConfig.Instance.StorageTargets)
			{
				m_storageTargets.Add(storageTarget);
			}
			InitializeComponent();

			StorageTargetDropdown.ItemsSource = m_storageTargets;
			StorageTargetDropdown.SelectedIndex = 0;
			m_storageTargetStringCached = (string)StorageTargetDropdown.SelectedValue;

			StorageTargetDropdown.SelectionChanged += OnStorageSelectionChanged;
		}

		private void OnStorageSelectionChanged(object a_sender, SelectionChangedEventArgs a_e)
		{
			m_storageTargetStringCached = (string)StorageTargetDropdown.SelectedValue;
			OnStorageTargetChanged.Invoke(StorageTargetString);
		}
	}
}
