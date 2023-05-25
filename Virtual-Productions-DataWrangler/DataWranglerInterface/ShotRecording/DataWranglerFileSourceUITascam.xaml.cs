using System.Windows;
using System.Windows.Controls;
using AutoNotify;
using DataWranglerCommon;
using DataWranglerCommon.IngestDataSources;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for DataWranglerFileSourceUIBlackmagicUrsa.xaml
	/// </summary>
	public partial class DataWranglerFileSourceUITascam : UserControl, IDataWranglerFileSourceUITitleProvider
	{
		public IngestDataSourceMetaTascam TargetMeta { get; private set; }

		public string FileSourceTitle => new IngestDataSourceMetaTascam().SourceType;

		public DataWranglerFileSourceUITascam(IngestDataSourceMetaTascam a_meta)
		{
			TargetMeta = a_meta;

			InitializeComponent();
		}

		private void ButtonDecreaseIndex_Click(object a_sender, RoutedEventArgs a_e)
		{
			TargetMeta.FileIndex -= 1;
		}

		private void ButtonIncreaseIndex_Click(object a_sender, RoutedEventArgs a_e)
		{
			TargetMeta.FileIndex += 1;
		}
	}
}
