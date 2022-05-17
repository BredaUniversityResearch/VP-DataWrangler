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

		[AutoNotify] private string m_storageTarget = "";

		public DataWranglerVideoMeta Clone()
		{
			return new DataWranglerVideoMeta
			{
				m_source = m_source, 
				m_codecName = m_codecName, 
				m_recordingStart = m_recordingStart,
				m_storageTarget = m_storageTarget
			};
		}
	}

	public class DataWranglerShotVersionMeta
	{
		public class AudioMeta
		{
			public string Source = "";

			public AudioMeta Clone()
			{
				return new AudioMeta
				{
					Source = Source
				};
			}
		};

		public DataWranglerVideoMeta Video = new DataWranglerVideoMeta();
		public AudioMeta Audio = new AudioMeta();

		public DataWranglerShotVersionMeta Clone()
		{
			DataWranglerShotVersionMeta clonedMeta = new DataWranglerShotVersionMeta
			{
				Video = Video.Clone(),
				Audio = Audio.Clone()
			};
			return clonedMeta;
		}
	}
}