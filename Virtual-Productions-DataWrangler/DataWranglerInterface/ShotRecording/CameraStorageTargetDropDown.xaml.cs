using System.Collections.ObjectModel;
using System.Windows.Controls;
using DataWranglerInterface.Configuration;

namespace DataWranglerInterface.ShotRecording
{
	public partial class CameraStorageTargetDropDown : UserControl
	{
		private readonly ObservableCollection<string> m_storageTargets = new ObservableCollection<string>();

		public string StorageTargetString
		{
			get => (string)StorageTargetDropdown.SelectedItem;
			set
			{
				int storageTargetIndex = m_storageTargets.IndexOf(value);
				if (storageTargetIndex == -1)
				{
					m_storageTargets.Add(value);
					storageTargetIndex = m_storageTargets.Count - 1;
				}

				StorageTargetDropdown.SelectedIndex = storageTargetIndex;
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

			StorageTargetDropdown.SelectionChanged += OnStorageSelectionChanged;
		}

		private void OnStorageSelectionChanged(object a_sender, SelectionChangedEventArgs a_e)
		{
			OnStorageTargetChanged.Invoke(StorageTargetString);
		}
	}
}
