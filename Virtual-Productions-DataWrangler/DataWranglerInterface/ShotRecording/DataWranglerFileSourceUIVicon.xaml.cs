using System.Windows.Controls;
using DataWranglerCommon;
using DataWranglerCommon.IngestDataSources;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for DataWranglerFileSourceUIBlackmagicUrsa.xaml
	/// </summary>
	public partial class DataWranglerFileSourceUIVicon : UserControl, IDataWranglerFileSourceUITitleProvider
	{
		public IngestDataSourceMetaViconTracking TargetMeta { get; private set; }

		public string FileSourceTitle => new IngestDataSourceMetaViconTracking().SourceType;

		public DataWranglerFileSourceUIVicon(IngestDataSourceMetaViconTracking a_meta)
		{
			TargetMeta = a_meta;

			InitializeComponent();
		}
	}
}
