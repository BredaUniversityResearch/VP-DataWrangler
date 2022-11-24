using System.Windows.Controls;
using AutoNotify;
using DataWranglerCommon;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for DataWranglerFileSourceUIBlackmagicUrsa.xaml
	/// </summary>
	public partial class DataWranglerFileSourceUIBlackmagicUrsa : UserControl, IDataWranglerFileSourceUITitleProvider
	{
		public DataWranglerFileSourceMetaBlackmagicUrsa TargetMeta { get; private set; }

		public string FileSourceTitle => "Blackmagic Ursa";

		public DataWranglerFileSourceUIBlackmagicUrsa(DataWranglerFileSourceMetaBlackmagicUrsa a_meta)
		{
			TargetMeta = a_meta;

			InitializeComponent();
		}

	}
}
