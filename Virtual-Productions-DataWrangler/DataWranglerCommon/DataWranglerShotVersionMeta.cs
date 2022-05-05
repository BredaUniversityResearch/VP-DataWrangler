using System.ComponentModel;
using System.Runtime.CompilerServices;
using AutoNotify;

namespace DataWranglerCommon
{
	public partial class DataWranglerVideoMeta
	{
		[AutoNotify]
		private string m_source = "";

		[AutoNotify]
		private string m_codecName = "";

		[AutoNotify] 
		private DateTimeOffset? m_recordingStart = null;

		[AutoNotify] private string m_storageTarget = null;
	}

	public class DataWranglerShotVersionMeta
	{
		public class AudioMeta
		{
			public string Source = "";
		};

		public DataWranglerVideoMeta Video = new DataWranglerVideoMeta();
		public AudioMeta Audio = new AudioMeta();
	}
}