using AutoNotify;
using DataApiCommon;

namespace DataWranglerInterface.ShotRecording
{
	public partial class ShotRecordingApplicationState
	{
		[AutoNotify]
		private DataEntityProject? m_selectedProject = null;
		[AutoNotify]
		private DataEntityShot? m_selectedShot = null;
		[AutoNotify]
		private DataEntityShotVersion? m_selectedShotVersion = null;
		[AutoNotify]
		private string m_nextPredictedShotVersionName = "";
	}
}
