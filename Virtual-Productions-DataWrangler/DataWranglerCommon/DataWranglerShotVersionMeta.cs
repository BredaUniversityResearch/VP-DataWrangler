using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DataWranglerCommon
{
	public class DataWranglerShotVersionMeta
	{
		public class VideoMeta: INotifyPropertyChanged
		{
			private string m_source = "";
			public string Source
			{
				get => m_source;
				set
				{
					m_source = value;
					OnPropertyChanged();
				}
			}
			
			public string CodecName { get; set; } = "";

			public event PropertyChangedEventHandler? PropertyChanged;

			protected virtual void OnPropertyChanged([CallerMemberName] string? a_propertyName = null)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(a_propertyName));
			}
		};

		public class AudioMeta
		{
			public string Source = "";
		};

		public VideoMeta Video = new VideoMeta();
		public AudioMeta Audio = new AudioMeta();
	}
}