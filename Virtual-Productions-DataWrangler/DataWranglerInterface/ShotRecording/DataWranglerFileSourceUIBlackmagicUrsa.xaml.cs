using System.Windows.Controls;
using AutoNotify;
using DataWranglerCommon;
using DataWranglerCommon.IngestDataSources;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for DataWranglerFileSourceUIBlackmagicUrsa.xaml
	/// </summary>
	public partial class DataWranglerFileSourceUIBlackmagicUrsa : UserControl, IDataWranglerFileSourceUITitleProvider
	{
		public IngestDataSourceMetaBlackmagicUrsa TargetMeta { get; private set; }

		public string FileSourceTitle => "Blackmagic Ursa";

		public DataWranglerFileSourceUIBlackmagicUrsa(IngestDataSourceMetaBlackmagicUrsa a_meta)
		{
			TargetMeta = a_meta;

			InitializeComponent();
		}

	}
}
