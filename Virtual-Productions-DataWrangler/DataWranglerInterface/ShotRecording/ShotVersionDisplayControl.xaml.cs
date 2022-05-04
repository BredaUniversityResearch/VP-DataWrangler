using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using DataWranglerCommon;
using DataWranglerInterface.DebugSupport;
using Newtonsoft.Json;
using ShotGridIntegration;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for ShotVersionDisplayControl.xaml
	/// </summary>
	public partial class ShotVersionDisplayControl : UserControl
	{
		private int SelectedShotId = -1;

		private ProjectSelectorControl? m_projectSelector = null;
		private DataWranglerShotVersionMeta m_currentVersionMeta = new DataWranglerShotVersionMeta();

		public ShotVersionDisplayControl()
		{
			InitializeComponent();

			VideoMetaControl.UpdateData(m_currentVersionMeta);

			CreateNewTake.Click += OnCreateNewShotVersion;
			
			AudioSource.Items.Add("Embedded");
			AudioSource.Items.Add("External Audio Recorder");
			AudioSource.SelectionChanged += OnAudioSourceChanged;

			VersionSelectorControl.OnShotVersionSelected += OnShotVersionSelected;
		}

		public void SetProjectSelector(ProjectSelectorControl a_projectSelector)
		{
			m_projectSelector = a_projectSelector;
		}

		private void OnCreateNewShotVersion(object a_sender, RoutedEventArgs a_e)
		{
			if (m_projectSelector == null)
			{
				throw new Exception();
			}

			if (SelectedShotId != -1)
			{
				int nextTakeId = VersionSelectorControl.GetHighestVersionNumber() + 1;

				VersionSelectorControl.BeginAddShotVersion();

				DataWranglerServiceProvider.Instance.ShotGridAPI.CreateNewShotVersion(m_projectSelector.SelectedProjectId, SelectedShotId,
					$"Take {nextTakeId:D2}").ContinueWith((Task<ShotGridEntityShotVersion?> a_result) =>
				{

					if (a_result.Result != null)
					{
						VersionSelectorControl.EndAddShotVersion(a_result.Result);
					}
				});
			}
		}

		public void OnShotSelected(int a_shotId)
		{
			SelectedShotId = a_shotId;
			VersionSelectorControl.AsyncRefreshShotVersion(a_shotId);
		}

		private void OnShotVersionSelected(ShotGridEntityShotVersion? a_shotVersion)
		{
			Debugger.Break(); //Handle case when no meta is assigned yet.
			if (a_shotVersion != null)
			{
				if (!string.IsNullOrEmpty(a_shotVersion.Attributes.DataWranglerMeta))
				{
					DataWranglerShotVersionMeta? shotMeta = JsonConvert.DeserializeObject<DataWranglerShotVersionMeta>(a_shotVersion.Attributes.DataWranglerMeta);
					if(shotMeta != null)
					{
						m_currentVersionMeta = shotMeta;
						VideoMetaControl.UpdateData(m_currentVersionMeta);
						SetValuesFromMeta(m_currentVersionMeta);

						m_currentVersionMeta.Video.PropertyChanged += OnAnyMetaPropertyChanged;
					}
					else
					{
						Logger.LogError("Interface", $"Failed to deserialize shot version meta from value: {a_shotVersion.Attributes.DataWranglerMeta}");
					}
				}
			}
		}

		private void OnAnyMetaPropertyChanged(object? a_sender, PropertyChangedEventArgs a_e)
		{
			DataWranglerServiceProvider.Instance.ShotGridAPI.UpdateEntitySingleProperty(EShotGridEntity.Shot, VersionSelectorControl.CurrentVersionEntityId, "Something", m_currentVersionMeta.ToString()!);
		}

		private void SetValuesFromMeta(DataWranglerShotVersionMeta a_meta)
		{
			AudioSource.SelectedItem = a_meta.Audio.Source;
		}

		private void OnAudioSourceChanged(object a_sender, SelectionChangedEventArgs a_e)
		{
			m_currentVersionMeta.Audio.Source = (string)(AudioSource.SelectedItem);
		}
	}
}
