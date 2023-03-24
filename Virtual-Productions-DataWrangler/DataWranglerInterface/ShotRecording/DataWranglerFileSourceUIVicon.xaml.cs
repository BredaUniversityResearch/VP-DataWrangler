using System.Windows.Controls;
using DataWranglerCommon;

namespace DataWranglerInterface.ShotRecording
{
	/// <summary>
	/// Interaction logic for DataWranglerFileSourceUIBlackmagicUrsa.xaml
	/// </summary>
	public partial class DataWranglerFileSourceUIVicon : UserControl, IDataWranglerFileSourceUITitleProvider
	{
		public DataWranglerFileSourceMetaViconTrackingData TargetMeta { get; private set; }

		public string FileSourceTitle => DataWranglerFileSourceMetaViconTrackingData.MetaSourceType;

		public DataWranglerFileSourceUIVicon(DataWranglerFileSourceMetaViconTrackingData a_meta)
		{
			TargetMeta = a_meta;

			InitializeComponent();
		}
	}
}
