using System.Windows;
using System.Windows.Controls;
using AutoNotify;
using DataWranglerCommon;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for DataWranglerFileSourceUIBlackmagicUrsa.xaml
	/// </summary>
	public partial class DataWranglerFileSourceUITascam : UserControl, IDataWranglerFileSourceUITitleProvider
	{
		public DataWranglerFileSourceMetaTascam TargetMeta { get; private set; }

		public string FileSourceTitle => DataWranglerFileSourceMetaTascam.MetaSourceType;

		public DataWranglerFileSourceUITascam(DataWranglerFileSourceMetaTascam a_meta)
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
